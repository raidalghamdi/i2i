using System.Text.Json;
using InnovationToImpact.Domain.Assignments;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Evaluations;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Evaluations;

public class EvaluationService : IEvaluationService
{
    private readonly InnovationDbContext _db;
    private readonly IEvaluationSettingsService _settings;

    public EvaluationService(InnovationDbContext db, IEvaluationSettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<EvaluationCommandResult> SubmitAsync(Guid ideaId, Guid evaluatorId, EvaluationInput input, CancellationToken cancellationToken = default)
    {
        var idea = await _db.Ideas.Include(i => i.IdeaStatus).SingleOrDefaultAsync(i => i.Id == ideaId, cancellationToken);
        if (idea is null) return new EvaluationCommandResult(EvaluationCommandStatus.NotFound);

        // Change 20260726 — checked ahead of the assignment gate so that a submitter who was wrongly
        // assigned to their own idea is still refused rather than admitted by the assignment.
        if (idea.SubmitterId == evaluatorId) return new EvaluationCommandResult(EvaluationCommandStatus.SelfAuthorship);

        // Per-idea assignment gate (replaces track membership).
        var assignment = await _db.Set<Assignment>()
            .Include(a => a.AssignmentStatus)
            .SingleOrDefaultAsync(a => a.IdeaId == ideaId && a.EvaluatorId == evaluatorId && a.Kind == AssignmentKinds.Evaluator, cancellationToken);
        if (assignment is null) return new EvaluationCommandResult(EvaluationCommandStatus.Forbidden);

        if (idea.IdeaStatus.Code != IdeaStatusCodes.Evaluation) return new EvaluationCommandResult(EvaluationCommandStatus.InvalidState);

        var isDraft = input.Action == EvaluationActions.Draft; // Change 20260726

        // An unsubmitted row is a resumable draft, so saving over it is expected; only a row that
        // has actually been submitted is off limits.
        var existing = await _db.Evaluations.SingleOrDefaultAsync( // Change 20260726
            e => e.IdeaId == ideaId && e.EvaluatorId == evaluatorId,
            cancellationToken);
        if (existing?.SubmittedAt is not null) return new EvaluationCommandResult(EvaluationCommandStatus.AlreadyEvaluated);

        var scores = input.CriteriaScores ?? new Dictionary<string, decimal>();
        var activeCodes = await _db.EvaluationCriteria // Change 20260726
            .Where(c => c.Active)
            .Select(c => c.Code)
            .ToListAsync(cancellationToken);

        // A submission must cover exactly the active criteria; a partial or stale set would make the
        // average silently incomparable between evaluators of the same idea. A draft is allowed to be
        // incomplete, but every key it does carry still has to name a live criterion.
        var criteriaValid = isDraft // Change 20260726
            ? scores.Keys.All(activeCodes.Contains)
            : scores.Count == activeCodes.Count && activeCodes.All(scores.ContainsKey);
        if (!criteriaValid)
        {
            return new EvaluationCommandResult(EvaluationCommandStatus.InvalidCriteria);
        }

        if (scores.Values.Any(s => s < EvaluationScoreRules.MinScore || s > EvaluationScoreRules.MaxScore)) // Change 20260726
        {
            return new EvaluationCommandResult(EvaluationCommandStatus.InvalidScore);
        }

        var average = scores.Count > 0 ? scores.Values.Average() : 0m; // Change 20260726
        var passThreshold = await _settings.GetPassThresholdAsync(cancellationToken);
        var recommendation = input.Recommendation is EvaluationRecommendationCodes.Pass or EvaluationRecommendationCodes.Fail // Change 20260726
            ? input.Recommendation
            : average >= passThreshold ? EvaluationRecommendationCodes.Pass : EvaluationRecommendationCodes.Fail;

        var criteriaScoresJson = JsonSerializer.Serialize(scores); // Change 20260726

        // Change 20260726 — upsert so repeated draft saves are idempotent instead of piling up rows.
        var evaluation = existing ?? new Evaluation
        {
            Id = Guid.NewGuid(),
            IdeaId = ideaId,
            EvaluatorId = evaluatorId,
        };
        evaluation.CriteriaScoresJson = criteriaScoresJson;
        evaluation.TotalScore = average;
        evaluation.Comments = input.Comments;
        evaluation.Recommendation = isDraft ? null : recommendation; // Change 20260726
        evaluation.ConflictOfInterest = input.ConflictOfInterest; // Change 20260726
        evaluation.SubmittedAt = isDraft ? null : DateTime.UtcNow; // Change 20260726
        if (existing is null) _db.Evaluations.Add(evaluation);

        // Change 20260726 — a draft leaves the assignment open and the idea untouched.
        if (isDraft)
        {
            await _db.SaveChangesAsync(cancellationToken);
            return new EvaluationCommandResult(EvaluationCommandStatus.Success, evaluation, idea);
        }

        // Mark this evaluator's assignment completed.
        var completedStatus = await _db.Set<AssignmentStatus>().SingleAsync(s => s.Code == AssignmentStatusCodes.Completed, cancellationToken);
        assignment.AssignmentStatusId = completedStatus.Id;
        assignment.AssignmentStatus = completedStatus;

        await _db.SaveChangesAsync(cancellationToken);

        // Quorum: have all assigned evaluators submitted? Change 20260726 — drafts don't count, and an
        // evaluator who declared a conflict of interest is removed from both the expected count and
        // the aggregate, so one recusal can't stall the idea.
        var assignedCount = await _db.Set<Assignment>()
            .CountAsync(a => a.IdeaId == ideaId && a.Kind == AssignmentKinds.Evaluator, cancellationToken);
        var submittedEvaluations = await _db.Evaluations // Change 20260726
            .Where(e => e.IdeaId == ideaId && e.SubmittedAt != null)
            .ToListAsync(cancellationToken);
        var counted = submittedEvaluations.Where(e => !e.ConflictOfInterest).ToList(); // Change 20260726
        var expected = assignedCount - submittedEvaluations.Count(e => e.ConflictOfInterest); // Change 20260726

        if (counted.Count >= expected) // Change 20260726
        {
            // Every assigned evaluator recused themselves; there is no score to aggregate and the
            // supervisor has to resolve it by hand.
            idea.EvaluationAggregateScore = counted.Count > 0 ? counted.Average(e => e.TotalScore) : null; // Change 20260726
            idea.EvaluationAggregateJson = counted.Count > 0 ? BuildAggregateJson(counted) : null; // Change 20260726

            var reviewStatus = await _db.IdeaStatuses.SingleAsync(s => s.Code == IdeaStatusCodes.EvaluationReview, cancellationToken);
            idea.IdeaStatusId = reviewStatus.Id;
            idea.IdeaStatus = reviewStatus;
            idea.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new EvaluationCommandResult(EvaluationCommandStatus.Success, evaluation, idea);
    }

    // Change 20260726 — criteria are now dynamic, so the code list comes from what was actually
    // scored rather than a fixed set. Historical rows keep resolving because seeded codes are unchanged.
    private static string BuildAggregateJson(IReadOnlyCollection<Evaluation> evaluations)
    {
        var parsedScores = evaluations
            .Select(e => JsonSerializer.Deserialize<Dictionary<string, decimal>>(e.CriteriaScoresJson) ?? new())
            .ToList();

        var means = new Dictionary<string, decimal>();
        foreach (var code in parsedScores.SelectMany(p => p.Keys).Distinct())
        {
            var vals = parsedScores.Where(p => p.ContainsKey(code)).Select(p => p[code]).ToList();
            means[code] = vals.Count > 0 ? vals.Average() : 0m;
        }
        return JsonSerializer.Serialize(means);
    }

    public async Task<IReadOnlyList<Idea>> GetQueueAsync(Guid evaluatorId, CancellationToken cancellationToken = default)
    {
        var assignedIdeaIds = await _db.Set<Assignment>()
            .Where(a => a.EvaluatorId == evaluatorId && a.Kind == AssignmentKinds.Evaluator)
            .Select(a => a.IdeaId)
            .ToListAsync(cancellationToken);

        return await _db.Ideas
            .Include(i => i.IdeaStatus)
            .Include(i => i.Submitter)
            .Where(i => assignedIdeaIds.Contains(i.Id) && i.IdeaStatus.Code == IdeaStatusCodes.Evaluation)
            .OrderBy(i => i.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Evaluation>> GetMyEvaluationsAsync(Guid evaluatorId, CancellationToken cancellationToken = default)
    {
        return await _db.Evaluations
            .Include(e => e.Idea)
            .Where(e => e.EvaluatorId == evaluatorId)
            .OrderByDescending(e => e.SubmittedAt)
            .ToListAsync(cancellationToken);
    }
}
