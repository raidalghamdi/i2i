using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Domain.Screening;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Screening;

public class ScreeningService : IScreeningService
{
    private const string Approve = "approve";
    private const string Reject = "reject";
    private const string Return = "return";
    private const int MinReturnReasonLength = 10;

    private readonly InnovationDbContext _db;

    public ScreeningService(InnovationDbContext db)
    {
        _db = db;
    }

    public async Task<ScreeningCommandResult> SubmitDecisionAsync(Guid ideaId, Guid supervisorId, ScreeningDecisionInput input, CancellationToken cancellationToken = default)
    {
        var idea = await _db.Ideas.Include(i => i.IdeaStatus).SingleOrDefaultAsync(i => i.Id == ideaId, cancellationToken);
        if (idea is null) return new ScreeningCommandResult(ScreeningCommandStatus.NotFound);
        if (idea.IdeaStatus.Code != IdeaStatusCodes.Submitted) return new ScreeningCommandResult(ScreeningCommandStatus.InvalidState);

        string nextStatusCode;
        string? reasonToStore = null;

        switch (input.DecisionCode)
        {
            case Approve:
                nextStatusCode = IdeaStatusCodes.Evaluation;
                break;
            case Reject:
                if (string.IsNullOrWhiteSpace(input.Reason)) return new ScreeningCommandResult(ScreeningCommandStatus.ReasonRequired);
                nextStatusCode = IdeaStatusCodes.Rejected;
                reasonToStore = input.Reason;
                break;
            case Return:
                if (string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length < MinReturnReasonLength) return new ScreeningCommandResult(ScreeningCommandStatus.ReasonRequired);
                if (input.EditableSections is { Count: > 0 } && input.EditableSections.Any(s => !IdeaSectionKeys.All.Contains(s)))
                {
                    return new ScreeningCommandResult(ScreeningCommandStatus.InvalidDecision);
                }
                nextStatusCode = IdeaStatusCodes.Returned;
                reasonToStore = input.Reason;
                break;
            default:
                return new ScreeningCommandResult(ScreeningCommandStatus.InvalidDecision);
        }

        var nextStatus = await _db.IdeaStatuses.SingleAsync(s => s.Code == nextStatusCode, cancellationToken);
        idea.IdeaStatusId = nextStatus.Id;
        idea.IdeaStatus = nextStatus;
        idea.ScreeningReason = reasonToStore;
        idea.EditableSections = input.DecisionCode == Return && input.EditableSections is { Count: > 0 }
            ? string.Join(',', input.EditableSections)
            : null;
        idea.UpdatedAt = DateTime.UtcNow;
        if (input.DecisionCode == Approve)
        {
            idea.EnteredEvaluationAt = idea.UpdatedAt;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new ScreeningCommandResult(ScreeningCommandStatus.Success, idea);
    }

    public async Task<IReadOnlyList<Idea>> GetQueueAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Ideas
            .Include(i => i.IdeaStatus)
            .Include(i => i.Submitter)
            .Where(i => i.IdeaStatus.Code == IdeaStatusCodes.Submitted)
            .OrderBy(i => i.UpdatedAt)
            .ToListAsync(cancellationToken);
    }
}
