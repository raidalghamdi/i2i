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

        // Per-idea assignment gate (replaces track membership).
        var assignment = await _db.Set<Assignment>()
            .Include(a => a.AssignmentStatus)
            .SingleOrDefaultAsync(a => a.IdeaId == ideaId && a.EvaluatorId == evaluatorId && a.Kind == AssignmentKinds.Evaluator, cancellationToken);
        if (assignment is null) return new EvaluationCommandResult(EvaluationCommandStatus.Forbidden);

        if (idea.IdeaStatus.Code != IdeaStatusCodes.Evaluation) return new EvaluationCommandResult(EvaluationCommandStatus.InvalidState);

        var alreadyEvaluated = await _db.Evaluations.AnyAsync(
            e => e.IdeaId == ideaId && e.EvaluatorId == evaluatorId,
            cancellationToken);
        if (alreadyEvaluated) return new EvaluationCommandResult(EvaluationCommandStatus.AlreadyEvaluated);

        var scores = input.CriteriaScores ?? new Dictionary<string, decimal>();
        var activeCodes = await _db.EvaluationCriteria // Change 20260726
            .Where(c => c.Active)
            .Select(c => c.Code)
            .ToListAsync(cancellationToken);

        // A submission must cover exactly the active criteria; a partial or stale set would make the
        // average silently incomparable between evaluators of the same idea.
        if (scores.Count != activeCodes.Count || activeCodes.Any(code => !scores.ContainsKey(code))) // Change 20260726
        {
            return new EvaluationCommandResult(EvaluationCommandStatus.InvalidCriteria);
        }

        if (scores.Values.Any(s => s < EvaluationScoreRules.MinScore || s > EvaluationScoreRules.MaxScore)) // Change 20260726
        {
            return new EvaluationCommandResult(EvaluationCommandStatus.InvalidScore);
        }

        var average = scores.Values.Average(); // Change 20260726
        var passThreshold = await _settings.GetPassThresholdAsync(cancellationToken);
        var recommendation = input.Recommendation is EvaluationRecommendationCodes.Pass or EvaluationRecommendationCodes.Fail // Change 20260726
            ? input.Recommendation
            : average >= passThreshold ? EvaluationRecommendationCodes.Pass : EvaluationRecommendationCodes.Fail;

        var criteriaScoresJson = JsonSerializer.Serialize(scores); // Change 20260726

        var evaluation = new Evaluation
        {
            Id = Guid.NewGuid(),
            IdeaId = ideaId,
            EvaluatorId = evaluatorId,
            CriteriaScoresJson = criteriaScoresJson,
            TotalScore = average,
            Comments = input.Comments,
            Recommendation = recommendation,
            SubmittedAt = DateTime.UtcNow,
        };
        _db.Evaluations.Add(evaluation);

        // Mark this evaluator's assignment completed.
        var completedStatus = await _db.Set<AssignmentStatus>().SingleAsync(s => s.Code == AssignmentStatusCodes.Completed, cancellationToken);
        assignment.AssignmentStatusId = completedStatus.Id;
        assignment.AssignmentStatus = completedStatus;

        await _db.SaveChangesAsync(cancellationToken);

        // Quorum: have all assigned evaluators submitted?
        var assignedCount = await _db.Set<Assignment>()
            .CountAsync(a => a.IdeaId == ideaId && a.Kind == AssignmentKinds.Evaluator, cancellationToken);
        var submittedCount = await _db.Evaluations.CountAsync(e => e.IdeaId == ideaId, cancellationToken);

        if (submittedCount >= assignedCount)
        {
            var all = await _db.Evaluations.Where(e => e.IdeaId == ideaId).ToListAsync(cancellationToken);
            idea.EvaluationAggregateScore = all.Average(e => e.TotalScore);
            idea.EvaluationAggregateJson = BuildAggregateJson(all);

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
