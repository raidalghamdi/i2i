using InnovationToImpact.Domain.Assignments;
using InnovationToImpact.Domain.Auth;
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

    private readonly IIdeaStatusNotifier _statusNotifier; // Change 20260726

    public ScreeningService(InnovationDbContext db, IIdeaStatusNotifier statusNotifier)
    {
        _db = db;
        _statusNotifier = statusNotifier; // Change 20260726
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
            {
                var evaluatorIds = input.EvaluatorIds?.Distinct().ToList() ?? new List<Guid>();
                if (evaluatorIds.Count == 0) return new ScreeningCommandResult(ScreeningCommandStatus.EvaluatorsRequired);

                var validEvaluatorCount = await _db.Users.CountAsync(
                    u => evaluatorIds.Contains(u.Id) && u.UserRoles.Any(ur => ur.Role.Code == RoleCodes.Evaluator),
                    cancellationToken);
                if (validEvaluatorCount != evaluatorIds.Count) return new ScreeningCommandResult(ScreeningCommandStatus.InvalidEvaluator);

                nextStatusCode = IdeaStatusCodes.Evaluation;
                break;
            }
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

            var pendingStatus = await _db.Set<AssignmentStatus>().SingleAsync(s => s.Code == AssignmentStatusCodes.Pending, cancellationToken);
            foreach (var evaluatorId in input.EvaluatorIds!.Distinct())
            {
                _db.Set<Assignment>().Add(new Assignment
                {
                    Id = Guid.NewGuid(),
                    IdeaId = idea.Id,
                    EvaluatorId = evaluatorId,
                    AssignedById = supervisorId,
                    AssignmentStatusId = pendingStatus.Id,
                    Kind = AssignmentKinds.Evaluator,
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _statusNotifier.NotifyStatusChangedAsync(idea, nextStatus.Code, cancellationToken); // Change 20260726

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
