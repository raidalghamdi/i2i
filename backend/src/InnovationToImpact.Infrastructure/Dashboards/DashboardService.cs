using InnovationToImpact.Domain.Dashboards;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Dashboards;

public class DashboardService : IDashboardService
{
    // Terminal (non-active) idea statuses — legacy excluded closed/archived/withdrawn/rejected;
    // the new schema's dead-end statuses are these three.
    private static readonly string[] TerminalStatuses =
        { IdeaStatusCodes.Rejected, IdeaStatusCodes.EvaluationFailed, IdeaStatusCodes.NotSelected };

    private static readonly string[] UnderReviewStatuses =
    {
        IdeaStatusCodes.Submitted, IdeaStatusCodes.Evaluation, IdeaStatusCodes.PassAwaitingAttachments,
        IdeaStatusCodes.Committee, IdeaStatusCodes.PendingFinalRanking,
    };
    private static readonly string[] ApprovedStatuses =
    {
        IdeaStatusCodes.Approved, IdeaStatusCodes.InPilot, IdeaStatusCodes.InMeasurement, IdeaStatusCodes.InScaling,
    };
    private static readonly string[] RejectedStatuses =
    {
        IdeaStatusCodes.Rejected, IdeaStatusCodes.EvaluationFailed, IdeaStatusCodes.NotSelected,
    };

    private readonly InnovationDbContext _db;
    public DashboardService(InnovationDbContext db) => _db = db;

    public async Task<AdminDashboard> GetAdminAsync(CancellationToken ct = default)
    {
        try
        {
            var totalUsers = await _db.Users.CountAsync(ct);
            var activeIdeas = await _db.Ideas.CountAsync(i => !TerminalStatuses.Contains(i.IdeaStatus.Code), ct);
            var pendingEvaluations = await _db.Assignments.CountAsync(a => a.AssignmentStatus.Code == "pending", ct);
            return new AdminDashboard(totalUsers, activeIdeas, pendingEvaluations, "Healthy");
        }
        catch
        {
            return new AdminDashboard(0, 0, 0, "Warning");
        }
    }

    public async Task<CommitteeDashboard> GetCommitteeAsync(CancellationToken ct = default)
    {
        var awaiting = await _db.Ideas.CountAsync(i => i.IdeaStatus.Code == IdeaStatusCodes.Committee, ct);
        var weekAgo = DateTime.UtcNow.AddDays(-7);
        var thisWeek = await _db.CommitteeDecisions.CountAsync(d => d.DecidedAt != null && d.DecidedAt >= weekAgo, ct);
        return new CommitteeDashboard(awaiting, thisWeek);
    }

    public async Task<SupervisorDashboard> GetSupervisorAsync(Guid userId, CancellationToken ct = default)
    {
        var me = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        var dept = me?.Department;

        var teamMembers = dept == null ? 0 : await _db.Users.CountAsync(u => u.Department == dept, ct);
        var sectorIdeas = dept == null ? 0 : await _db.Ideas.CountAsync(i => i.Submitter.Department == dept, ct);
        var escalations = await _db.Escalations.CountAsync(e => e.OwnerId == userId && e.EscalationStatus.Code == "open", ct);

        var total = await _db.Ideas.CountAsync(i => i.IdeaStatus.Code != IdeaStatusCodes.Draft, ct);
        var underReview = await _db.Ideas.CountAsync(i => UnderReviewStatuses.Contains(i.IdeaStatus.Code), ct);
        var approved = await _db.Ideas.CountAsync(i => ApprovedStatuses.Contains(i.IdeaStatus.Code), ct);
        var returned = await _db.Ideas.CountAsync(i => i.IdeaStatus.Code == IdeaStatusCodes.Returned, ct);
        var rejected = await _db.Ideas.CountAsync(i => RejectedStatuses.Contains(i.IdeaStatus.Code), ct);

        return new SupervisorDashboard(teamMembers, sectorIdeas, escalations,
            new ScreeningBuckets(total, underReview, approved, returned, rejected));
    }
}
