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

        var scores = new[] { input.Innovation, input.Impact, input.Execution, input.Scalability, input.Presentation };
        if (scores.Any(s => s < EvaluationScoreRules.MinScore || s > EvaluationScoreRules.MaxScore))
        {
            return new EvaluationCommandResult(EvaluationCommandStatus.InvalidScore);
        }

        var average = scores.Average();
        var passThreshold = await _settings.GetPassThresholdAsync(cancellationToken);
        var recommendation = average >= passThreshold ? EvaluationRecommendationCodes.Pass : EvaluationRecommendationCodes.Fail;

        var criteriaScoresJson = JsonSerializer.Serialize(new Dictionary<string, decimal>
        {
            [EvaluationCriteriaCodes.Innovation] = input.Innovation,
            [EvaluationCriteriaCodes.Impact] = input.Impact,
            [EvaluationCriteriaCodes.Execution] = input.Execution,
            [EvaluationCriteriaCodes.Scalability] = input.Scalability,
            [EvaluationCriteriaCodes.Presentation] = input.Presentation,
        });

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

    private static string BuildAggregateJson(IReadOnlyCollection<Evaluation> evaluations)
    {
        var codes = new[]
        {
            EvaluationCriteriaCodes.Innovation, EvaluationCriteriaCodes.Impact,
            EvaluationCriteriaCodes.Execution, EvaluationCriteriaCodes.Scalability,
            EvaluationCriteriaCodes.Presentation,
        };
        var means = new Dictionary<string, decimal>();
        foreach (var code in codes)
        {
            var vals = new List<decimal>();
            foreach (var e in evaluations)
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, decimal>>(e.CriteriaScoresJson) ?? new();
                if (parsed.TryGetValue(code, out var v)) vals.Add(v);
            }
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
