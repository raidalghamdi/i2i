using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.EvaluationReview;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.EvaluationReview;

public class EvaluationReviewService : IEvaluationReviewService
{
    private const int MinReturnReasonLength = 10;

    private readonly InnovationDbContext _db;

    public EvaluationReviewService(InnovationDbContext db)
    {
        _db = db;
    }

    public async Task<EvaluationReviewCommandResult> SubmitDecisionAsync(Guid ideaId, Guid supervisorId, EvaluationReviewDecisionInput input, CancellationToken cancellationToken = default)
    {
        var idea = await _db.Ideas.Include(i => i.IdeaStatus).SingleOrDefaultAsync(i => i.Id == ideaId, cancellationToken);
        if (idea is null) return new EvaluationReviewCommandResult(EvaluationReviewCommandStatus.NotFound);
        if (idea.IdeaStatus.Code != IdeaStatusCodes.EvaluationReview) return new EvaluationReviewCommandResult(EvaluationReviewCommandStatus.InvalidState);

        string nextStatusCode;

        switch (input.DecisionCode)
        {
            case EvaluationReviewDecisionCodes.Forward:
                idea.SupervisorEvaluationComment = input.SupervisorComment;
                nextStatusCode = IdeaStatusCodes.SubmitterReview;
                break;

            case EvaluationReviewDecisionCodes.Return:
                if (string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length < MinReturnReasonLength)
                {
                    return new EvaluationReviewCommandResult(EvaluationReviewCommandStatus.ReasonRequired);
                }
                if (input.EditableSections is { Count: > 0 } && input.EditableSections.Any(s => !IdeaSectionKeys.All.Contains(s)))
                {
                    return new EvaluationReviewCommandResult(EvaluationReviewCommandStatus.InvalidDecision);
                }
                idea.ScreeningReason = input.Reason;
                idea.EditableSections = input.EditableSections is { Count: > 0 }
                    ? string.Join(',', input.EditableSections)
                    : null;
                nextStatusCode = IdeaStatusCodes.Returned;
                break;

            case EvaluationReviewDecisionCodes.Fail:
                idea.ScreeningReason = input.Reason;
                nextStatusCode = IdeaStatusCodes.EvaluationFailed;
                break;

            default:
                return new EvaluationReviewCommandResult(EvaluationReviewCommandStatus.InvalidDecision);
        }

        if (!IdeaWorkflow.CanTransition(idea.IdeaStatus.Code, nextStatusCode))
        {
            return new EvaluationReviewCommandResult(EvaluationReviewCommandStatus.InvalidState);
        }

        var nextStatus = await _db.IdeaStatuses.SingleAsync(s => s.Code == nextStatusCode, cancellationToken);
        idea.IdeaStatusId = nextStatus.Id;
        idea.IdeaStatus = nextStatus;
        idea.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return new EvaluationReviewCommandResult(EvaluationReviewCommandStatus.Success, idea);
    }

    public async Task<IReadOnlyList<Idea>> GetReviewQueueAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Ideas
            .Include(i => i.IdeaStatus)
            .Include(i => i.Submitter)
            .Where(i => i.IdeaStatus.Code == IdeaStatusCodes.EvaluationReview)
            .OrderBy(i => i.UpdatedAt)
            .ToListAsync(cancellationToken);
    }
}
