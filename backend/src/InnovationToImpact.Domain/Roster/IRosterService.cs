namespace InnovationToImpact.Domain.Roster;

public interface IRosterService
{
    Task<IReadOnlyList<RosterHubRow>> GetHubAsync(CancellationToken cancellationToken = default);
    Task<RosterRoleDetail?> GetRoleDetailAsync(string roleCode, CancellationToken cancellationToken = default);
    Task<RoleInvitationCommandResult> CreateInvitationAsync(RoleInvitationCreateInput input, Guid actorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoleInvitationCommandResult>> BulkCreateInvitationsAsync(IReadOnlyList<RoleInvitationCreateInput> inputs, Guid actorId, CancellationToken cancellationToken = default);
    Task<RoleInvitationCommandResult> WithdrawAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoleInvitationCommandResult>> BulkWithdrawAsync(IReadOnlyList<Guid> ids, Guid actorId, CancellationToken cancellationToken = default);
    Task<RoleInvitationCommandResult> RemindAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoleInvitationCommandResult>> BulkRemindAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default);
}
