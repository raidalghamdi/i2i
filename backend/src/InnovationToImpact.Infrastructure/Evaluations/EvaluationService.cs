using System.Text.Json;
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

        var isAssignedToTrack = await _db.EvaluatorTrackAssignments.AnyAsync(
            a => a.EvaluatorId == evaluatorId && a.TrackId == idea.StrategicThemeId,
            cancellationToken);
        if (!isAssignedToTrack) return new EvaluationCommandResult(EvaluationCommandStatus.Forbidden);

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

        var outcomeStatusCode = recommendation == EvaluationRecommendationCodes.Pass
            ? IdeaStatusCodes.PassAwaitingAttachments
            : IdeaStatusCodes.EvaluationFailed;
        var outcomeStatus = await _db.IdeaStatuses.SingleAsync(s => s.Code == outcomeStatusCode, cancellationToken);
        idea.IdeaStatusId = outcomeStatus.Id;
        idea.IdeaStatus = outcomeStatus;
        idea.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return new EvaluationCommandResult(EvaluationCommandStatus.Success, evaluation, idea);
    }

    public async Task<IReadOnlyList<Idea>> GetQueueAsync(Guid evaluatorId, CancellationToken cancellationToken = default)
    {
        var trackIds = await _db.EvaluatorTrackAssignments
            .Where(a => a.EvaluatorId == evaluatorId)
            .Select(a => a.TrackId)
            .ToListAsync(cancellationToken);

        return await _db.Ideas
            .Include(i => i.IdeaStatus)
            .Include(i => i.Submitter)
            .Where(i => trackIds.Contains(i.StrategicThemeId) && i.IdeaStatus.Code == IdeaStatusCodes.Evaluation)
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
