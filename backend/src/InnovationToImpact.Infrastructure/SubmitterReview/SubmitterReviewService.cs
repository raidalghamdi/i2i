using InnovationToImpact.Domain.Evaluations;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Domain.SubmitterReview;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.SubmitterReview;

public class SubmitterReviewService : ISubmitterReviewService
{
    private readonly InnovationDbContext _db;
    private readonly IEvaluationSettingsService _settings;

    public SubmitterReviewService(InnovationDbContext db, IEvaluationSettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<SubmitterReviewCommandResult> ResubmitAsync(Guid ideaId, Guid submitterId, ResubmitEvaluationInput input, CancellationToken cancellationToken = default)
    {
        var idea = await _db.Ideas.Include(i => i.IdeaStatus).SingleOrDefaultAsync(i => i.Id == ideaId, cancellationToken);
        if (idea is null) return new SubmitterReviewCommandResult(SubmitterReviewCommandStatus.NotFound);
        if (idea.SubmitterId != submitterId) return new SubmitterReviewCommandResult(SubmitterReviewCommandStatus.Forbidden);
        if (idea.IdeaStatus.Code != IdeaStatusCodes.SubmitterReview) return new SubmitterReviewCommandResult(SubmitterReviewCommandStatus.InvalidState);
        if (string.IsNullOrWhiteSpace(input.Comment)) return new SubmitterReviewCommandResult(SubmitterReviewCommandStatus.CommentRequired);

        var passThreshold = await _settings.GetPassThresholdAsync(cancellationToken);
        if (idea.EvaluationAggregateScore is null || idea.EvaluationAggregateScore < passThreshold)
        {
            return new SubmitterReviewCommandResult(SubmitterReviewCommandStatus.BelowTarget);
        }

        idea.SubmitterResubmitComment = input.Comment;

        var nextStatus = await _db.IdeaStatuses.SingleAsync(s => s.Code == IdeaStatusCodes.CommitteePending, cancellationToken);
        idea.IdeaStatusId = nextStatus.Id;
        idea.IdeaStatus = nextStatus;
        idea.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return new SubmitterReviewCommandResult(SubmitterReviewCommandStatus.Success, idea);
    }
}
