using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.UserManagement;

public interface IUserManagementService
{
    Task<RoleGrantCommandResult> GrantRoleAsync(RoleGrantInput input, Guid grantedById, CancellationToken cancellationToken = default);
    Task<GroupGrantResult> GrantRoleToGroupAsync(string groupName, string roleCode, Guid grantedById, CancellationToken cancellationToken = default);
    Task<UserManagementCommandResult> RevokeRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task<UserManagementCommandResult> SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> ListUsersAsync(CancellationToken cancellationToken = default);
    Task<User?> GetUserDetailAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PendingRoleGrant>> ListPendingGrantsAsync(CancellationToken cancellationToken = default);
    Task<UserManagementCommandResult> CancelPendingGrantAsync(Guid pendingGrantId, CancellationToken cancellationToken = default);
}
