using InnovationToImpact.Domain.Assignments;
using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.CommitteeReferral;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.CommitteeReferral;

public class CommitteeReferralService : ICommitteeReferralService
{
    private readonly InnovationDbContext _db;

    private readonly IIdeaStatusNotifier _statusNotifier; // Change 20260726

    public CommitteeReferralService(InnovationDbContext db, IIdeaStatusNotifier statusNotifier)
    {
        _db = db;
        _statusNotifier = statusNotifier; // Change 20260726
    }

    public async Task<CommitteeReferralCommandResult> SubmitToCommitteeAsync(Guid ideaId, Guid supervisorId, SubmitToCommitteeInput input, CancellationToken cancellationToken = default)
    {
        var idea = await _db.Ideas.Include(i => i.IdeaStatus).SingleOrDefaultAsync(i => i.Id == ideaId, cancellationToken);
        if (idea is null) return new CommitteeReferralCommandResult(CommitteeReferralCommandStatus.NotFound);
        if (idea.IdeaStatus.Code != IdeaStatusCodes.CommitteePending) return new CommitteeReferralCommandResult(CommitteeReferralCommandStatus.InvalidState);

        var judgeIds = input.JudgeIds?.Distinct().ToList() ?? new List<Guid>();
        if (judgeIds.Count == 0) return new CommitteeReferralCommandResult(CommitteeReferralCommandStatus.JudgesRequired);

        var validJudgeCount = await _db.Users.CountAsync(
            u => judgeIds.Contains(u.Id) && u.UserRoles.Any(ur => ur.Role.Code == RoleCodes.Judge),
            cancellationToken);
        if (validJudgeCount != judgeIds.Count) return new CommitteeReferralCommandResult(CommitteeReferralCommandStatus.InvalidJudge);

        var pendingStatus = await _db.Set<AssignmentStatus>().SingleAsync(s => s.Code == AssignmentStatusCodes.Pending, cancellationToken);
        foreach (var judgeId in judgeIds)
        {
            _db.Set<Assignment>().Add(new Assignment
            {
                Id = Guid.NewGuid(),
                IdeaId = idea.Id,
                EvaluatorId = judgeId,
                AssignedById = supervisorId,
                AssignmentStatusId = pendingStatus.Id,
                Kind = AssignmentKinds.Judge,
            });
        }

        var committeeStatus = await _db.IdeaStatuses.SingleAsync(s => s.Code == IdeaStatusCodes.Committee, cancellationToken);
        idea.IdeaStatusId = committeeStatus.Id;
        idea.IdeaStatus = committeeStatus;
        idea.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        await _statusNotifier.NotifyStatusChangedAsync(idea, committeeStatus.Code, cancellationToken); // Change 20260726

        return new CommitteeReferralCommandResult(CommitteeReferralCommandStatus.Success, idea);
    }

    public async Task<IReadOnlyList<Idea>> GetPendingQueueAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Ideas
            .Include(i => i.IdeaStatus)
            .Include(i => i.Submitter)
            .Where(i => i.IdeaStatus.Code == IdeaStatusCodes.CommitteePending)
            .OrderBy(i => i.UpdatedAt)
            .ToListAsync(cancellationToken);
    }
}
