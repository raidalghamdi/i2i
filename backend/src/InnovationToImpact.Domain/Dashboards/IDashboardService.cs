namespace InnovationToImpact.Domain.Dashboards;

public sealed record AdminDashboard(int TotalUsers, int ActiveIdeas, int PendingEvaluations, string Health);
public sealed record CommitteeDashboard(int AwaitingDecision, int DecisionsThisWeek);
public sealed record ScreeningBuckets(int Total, int UnderReview, int Approved, int Returned, int Rejected);
public sealed record SupervisorDashboard(int TeamMembers, int SectorIdeas, int EscalationsAwaitingMe, ScreeningBuckets Screening);

public interface IDashboardService
{
    Task<AdminDashboard> GetAdminAsync(CancellationToken ct = default);
    Task<CommitteeDashboard> GetCommitteeAsync(CancellationToken ct = default);
    Task<SupervisorDashboard> GetSupervisorAsync(Guid userId, CancellationToken ct = default);
}
