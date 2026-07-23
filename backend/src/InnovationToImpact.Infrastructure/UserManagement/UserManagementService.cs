using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.UserManagement;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.UserManagement;

public class UserManagementService : IUserManagementService
{
    private readonly InnovationDbContext _db;
    private readonly IAdIdentityLookupService _lookupService;

    public UserManagementService(InnovationDbContext db, IAdIdentityLookupService lookupService)
    {
        _db = db;
        _lookupService = lookupService;
    }

    public async Task<RoleGrantCommandResult> GrantRoleAsync(RoleGrantInput input, Guid grantedById, CancellationToken cancellationToken = default)
    {
        var adIdentity = await _lookupService.ResolveAsync(input.SamAccountName, cancellationToken);
        if (adIdentity is null) return new RoleGrantCommandResult(RoleGrantCommandStatus.AdUserNotFound);

        var role = await _db.Roles.SingleOrDefaultAsync(r => r.Code == input.RoleCode, cancellationToken);
        if (role is null) return new RoleGrantCommandResult(RoleGrantCommandStatus.RoleNotFound);

        var user = await _db.Users.Include(u => u.UserRoles).SingleOrDefaultAsync(u => u.SamAccountName == input.SamAccountName, cancellationToken);

        if (user is not null)
        {
            var alreadyGranted = user.UserRoles.Any(ur => ur.RoleId == role.Id);
            if (alreadyGranted) return new RoleGrantCommandResult(RoleGrantCommandStatus.AlreadyGranted);

            user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, Role = role, IsPrimary = false });
            await _db.SaveChangesAsync(cancellationToken);
            return new RoleGrantCommandResult(RoleGrantCommandStatus.GrantedImmediately, User: user);
        }

        var alreadyPending = await _db.PendingRoleGrants.AnyAsync(g => g.SamAccountName == input.SamAccountName && g.RoleId == role.Id, cancellationToken);
        if (alreadyPending) return new RoleGrantCommandResult(RoleGrantCommandStatus.AlreadyPending);

        var pendingGrant = new PendingRoleGrant
        {
            Id = Guid.NewGuid(),
            SamAccountName = input.SamAccountName,
            RoleId = role.Id,
            GrantedById = grantedById,
        };
        _db.PendingRoleGrants.Add(pendingGrant);
        await _db.SaveChangesAsync(cancellationToken);
        return new RoleGrantCommandResult(RoleGrantCommandStatus.Pending, PendingGrant: pendingGrant);
    }

    public async Task<GroupGrantResult> GrantRoleToGroupAsync(string groupName, string roleCode, Guid grantedById, CancellationToken cancellationToken = default)
    {
        var members = await _lookupService.ResolveGroupMembersAsync(groupName, cancellationToken);

        var grantedCount = 0;
        var pendingCount = 0;
        var skippedCount = 0;
        var errors = new List<string>();

        foreach (var member in members)
        {
            var result = await GrantRoleAsync(new RoleGrantInput(member.SamAccountName, roleCode), grantedById, cancellationToken);
            switch (result.Status)
            {
                case RoleGrantCommandStatus.GrantedImmediately:
                    grantedCount++;
                    break;
                case RoleGrantCommandStatus.Pending:
                    pendingCount++;
                    break;
                case RoleGrantCommandStatus.AlreadyGranted:
                case RoleGrantCommandStatus.AlreadyPending:
                    skippedCount++;
                    break;
                default:
                    skippedCount++;
                    errors.Add($"{member.SamAccountName}: {result.Status}");
                    break;
            }
        }

        return new GroupGrantResult(grantedCount, pendingCount, skippedCount, errors);
    }

    public async Task<UserManagementCommandResult> RevokeRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var userRole = await _db.Set<UserRole>().SingleOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
        if (userRole is null) return new UserManagementCommandResult(UserManagementCommandStatus.NotFound);

        _db.Set<UserRole>().Remove(userRole);
        await _db.SaveChangesAsync(cancellationToken);
        return new UserManagementCommandResult(UserManagementCommandStatus.Success);
    }

    public async Task<UserManagementCommandResult> SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) return new UserManagementCommandResult(UserManagementCommandStatus.NotFound);

        user.IsActive = isActive;
        await _db.SaveChangesAsync(cancellationToken);
        return new UserManagementCommandResult(UserManagementCommandStatus.Success);
    }

    public async Task<IReadOnlyList<User>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .OrderBy(u => u.FullNameEn)
            .ToListAsync(cancellationToken);
    }

    public async Task<User?> GetUserDetailAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<PendingRoleGrant>> ListPendingGrantsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.PendingRoleGrants
            .Include(g => g.Role)
            .Include(g => g.GrantedBy)
            .OrderBy(g => g.GrantedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserManagementCommandResult> CancelPendingGrantAsync(Guid pendingGrantId, CancellationToken cancellationToken = default)
    {
        var grant = await _db.PendingRoleGrants.SingleOrDefaultAsync(g => g.Id == pendingGrantId, cancellationToken);
        if (grant is null) return new UserManagementCommandResult(UserManagementCommandStatus.NotFound);

        _db.PendingRoleGrants.Remove(grant);
        await _db.SaveChangesAsync(cancellationToken);
        return new UserManagementCommandResult(UserManagementCommandStatus.Success);
    }
}
