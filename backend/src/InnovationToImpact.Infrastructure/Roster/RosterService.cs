using System.Text.Json;
using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Email;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Roster;
using InnovationToImpact.Domain.UserManagement;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Roster;

public class RosterService : IRosterService
{
    private readonly InnovationDbContext _db;
    private readonly IUserManagementService _userManagementService;
    private readonly IAdIdentityLookupService _adIdentityLookupService;
    private readonly IAuditLogWriter _auditLogWriter;

    public RosterService(
        InnovationDbContext db,
        IUserManagementService userManagementService,
        IAdIdentityLookupService adIdentityLookupService,
        IAuditLogWriter auditLogWriter)
    {
        _db = db;
        _userManagementService = userManagementService;
        _adIdentityLookupService = adIdentityLookupService;
        _auditLogWriter = auditLogWriter;
    }

    public async Task<IReadOnlyList<RosterHubRow>> GetHubAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _db.Roles.OrderBy(r => r.SortOrder).ToListAsync(cancellationToken);
        var pendingStatusId = await StatusIdAsync(RoleInvitationStatusCodes.Pending, cancellationToken);
        var expiredStatusId = await StatusIdAsync(RoleInvitationStatusCodes.Expired, cancellationToken);
        var withdrawnStatusId = await StatusIdAsync(RoleInvitationStatusCodes.Withdrawn, cancellationToken);

        var activeCounts = await _db.Set<UserRole>()
            .Where(ur => ur.User.IsActive)
            .GroupBy(ur => ur.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);

        var pendingCounts = await CountsByRoleAsync(pendingStatusId, cancellationToken);
        var expiredCounts = await CountsByRoleAsync(expiredStatusId, cancellationToken);
        var withdrawnCounts = await CountsByRoleAsync(withdrawnStatusId, cancellationToken);

        return roles.Select(role => new RosterHubRow(
            role.Code, role.NameAr, role.NameEn,
            activeCounts.GetValueOrDefault(role.Id),
            pendingCounts.GetValueOrDefault(role.Id),
            expiredCounts.GetValueOrDefault(role.Id),
            withdrawnCounts.GetValueOrDefault(role.Id)
        )).ToList();
    }

    private async Task<Dictionary<Guid, int>> CountsByRoleAsync(Guid statusId, CancellationToken cancellationToken) =>
        await _db.RoleInvitations
            .Where(ri => ri.RoleInvitationStatusId == statusId)
            .GroupBy(ri => ri.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);

    public async Task<RosterRoleDetail?> GetRoleDetailAsync(string roleCode, CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles.SingleOrDefaultAsync(r => r.Code == roleCode, cancellationToken);
        if (role is null) return null;

        var activeMembers = await _db.Set<UserRole>()
            .Where(ur => ur.RoleId == role.Id)
            .Select(ur => ur.User)
            .OrderBy(u => u.FullNameEn)
            .Select(u => new RosterActiveMember(u.Id, u.SamAccountName, u.FullNameAr, u.FullNameEn, u.Email, u.IsActive))
            .ToListAsync(cancellationToken);

        var invitations = await _db.RoleInvitations
            .Include(ri => ri.RoleInvitationStatus)
            .Include(ri => ri.InvitedBy)
            .Where(ri => ri.RoleId == role.Id)
            .OrderByDescending(ri => ri.CreatedAt)
            .ToListAsync(cancellationToken);

        return new RosterRoleDetail(role.Code, role.NameAr, role.NameEn, activeMembers, invitations);
    }

    public async Task<RoleInvitationCommandResult> CreateInvitationAsync(RoleInvitationCreateInput input, Guid actorId, CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles.SingleOrDefaultAsync(r => r.Code == input.RoleCode, cancellationToken);
        if (role is null) return new RoleInvitationCommandResult(RoleInvitationCommandStatus.RoleNotFound, SamAccountName: input.SamAccountName);

        var pendingStatusId = await StatusIdAsync(RoleInvitationStatusCodes.Pending, cancellationToken);
        var existingPending = await _db.RoleInvitations.AnyAsync(
            ri => ri.SamAccountName == input.SamAccountName && ri.RoleId == role.Id && ri.RoleInvitationStatusId == pendingStatusId,
            cancellationToken);
        if (existingPending) return new RoleInvitationCommandResult(RoleInvitationCommandStatus.AlreadyPending, SamAccountName: input.SamAccountName);

        var grantResult = await _userManagementService.GrantRoleAsync(new RoleGrantInput(input.SamAccountName, input.RoleCode), actorId, cancellationToken);

        switch (grantResult.Status)
        {
            case RoleGrantCommandStatus.AdUserNotFound:
                return new RoleInvitationCommandResult(RoleInvitationCommandStatus.AdUserNotFound, SamAccountName: input.SamAccountName);
            case RoleGrantCommandStatus.RoleNotFound:
                return new RoleInvitationCommandResult(RoleInvitationCommandStatus.RoleNotFound, SamAccountName: input.SamAccountName);
            case RoleGrantCommandStatus.AlreadyGranted:
                return new RoleInvitationCommandResult(RoleInvitationCommandStatus.AlreadyApplied, SamAccountName: input.SamAccountName);
        }

        // Falls through for GrantedImmediately, Pending, and AlreadyPending: in the AlreadyPending case
        // (a PendingRoleGrant already exists for this SamAccountName+Role but was created by some other
        // admin flow, not by Roster — e.g. single-grant/group-grant) there is deliberately no existing
        // RoleInvitation row yet (we already checked for one above), so we "adopt" the pre-existing grant
        // by creating a new Pending RoleInvitation row to track/remind on it. See the design spec's
        // Decision 2 for the full rationale.
        var isImmediate = grantResult.Status == RoleGrantCommandStatus.GrantedImmediately;
        var adIdentity = await _adIdentityLookupService.ResolveAsync(input.SamAccountName, cancellationToken);
        var now = DateTime.UtcNow;

        DateTime? deadlineAt = null;
        if (!isImmediate)
        {
            var settings = await _db.RoleInvitationSettings.SingleAsync(cancellationToken);
            deadlineAt = input.DeadlineAt ?? now.AddDays(settings.DefaultExpiresDays);
        }

        var invitation = new RoleInvitation
        {
            Id = Guid.NewGuid(),
            SamAccountName = input.SamAccountName,
            RoleId = role.Id,
            DisplayName = adIdentity?.DisplayName,
            Email = adIdentity?.Email,
            RoleInvitationStatusId = isImmediate ? await StatusIdAsync(RoleInvitationStatusCodes.Applied, cancellationToken) : pendingStatusId,
            DeadlineAt = deadlineAt,
            RespondedAt = isImmediate ? now : null,
            ReminderCount = 0,
            Source = input.Source,
            InvitedById = actorId,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.RoleInvitations.Add(invitation);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("roleInvitation", invitation.Id, "roleInvitation.created", actorId, JsonSerializer.Serialize(input), cancellationToken);

        return new RoleInvitationCommandResult(RoleInvitationCommandStatus.Success, invitation, input.SamAccountName);
    }

    public async Task<IReadOnlyList<RoleInvitationCommandResult>> BulkCreateInvitationsAsync(IReadOnlyList<RoleInvitationCreateInput> inputs, Guid actorId, CancellationToken cancellationToken = default)
    {
        var results = new List<RoleInvitationCommandResult>();
        foreach (var input in inputs)
        {
            results.Add(await CreateInvitationAsync(input, actorId, cancellationToken));
        }
        return results;
    }

    public async Task<RoleInvitationCommandResult> WithdrawAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default)
    {
        var invitation = await _db.RoleInvitations.Include(ri => ri.Role).SingleOrDefaultAsync(ri => ri.Id == id, cancellationToken);
        if (invitation is null) return new RoleInvitationCommandResult(RoleInvitationCommandStatus.NotFound);

        var pendingStatusId = await StatusIdAsync(RoleInvitationStatusCodes.Pending, cancellationToken);
        if (invitation.RoleInvitationStatusId != pendingStatusId)
            return new RoleInvitationCommandResult(RoleInvitationCommandStatus.InvalidStatus, invitation);

        var pendingGrant = await _db.PendingRoleGrants.SingleOrDefaultAsync(
            g => g.SamAccountName == invitation.SamAccountName && g.RoleId == invitation.RoleId,
            cancellationToken);
        if (pendingGrant is not null) _db.PendingRoleGrants.Remove(pendingGrant);

        invitation.RoleInvitationStatusId = await StatusIdAsync(RoleInvitationStatusCodes.Withdrawn, cancellationToken);
        invitation.RespondedAt = DateTime.UtcNow;
        invitation.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("roleInvitation", invitation.Id, "roleInvitation.withdrawn", actorId, null, cancellationToken);

        return new RoleInvitationCommandResult(RoleInvitationCommandStatus.Success, invitation);
    }

    public async Task<IReadOnlyList<RoleInvitationCommandResult>> BulkWithdrawAsync(IReadOnlyList<Guid> ids, Guid actorId, CancellationToken cancellationToken = default)
    {
        var results = new List<RoleInvitationCommandResult>();
        foreach (var id in ids)
        {
            results.Add(await WithdrawAsync(id, actorId, cancellationToken));
        }
        return results;
    }

    public async Task<RoleInvitationCommandResult> RemindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invitation = await _db.RoleInvitations.Include(ri => ri.Role).SingleOrDefaultAsync(ri => ri.Id == id, cancellationToken);
        if (invitation is null) return new RoleInvitationCommandResult(RoleInvitationCommandStatus.NotFound);

        var pendingStatusId = await StatusIdAsync(RoleInvitationStatusCodes.Pending, cancellationToken);
        if (invitation.RoleInvitationStatusId != pendingStatusId)
            return new RoleInvitationCommandResult(RoleInvitationCommandStatus.InvalidStatus, invitation);

        var settings = await _db.RoleInvitationSettings.SingleAsync(cancellationToken);
        if (invitation.ReminderCount >= settings.MaxReminders)
            return new RoleInvitationCommandResult(RoleInvitationCommandStatus.InvalidStatus, invitation);

        await QueueReminderEmailAsync(invitation, cancellationToken);
        invitation.ReminderCount++;
        invitation.LastReminderAt = DateTime.UtcNow;
        invitation.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new RoleInvitationCommandResult(RoleInvitationCommandStatus.Success, invitation);
    }

    public async Task<IReadOnlyList<RoleInvitationCommandResult>> BulkRemindAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default)
    {
        var results = new List<RoleInvitationCommandResult>();
        foreach (var id in ids)
        {
            results.Add(await RemindAsync(id, cancellationToken));
        }
        return results;
    }

    private async Task QueueReminderEmailAsync(RoleInvitation invitation, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(invitation.Email)) return;

        var emailPendingStatus = await _db.EmailOutboxStatuses.SingleAsync(s => s.Code == EmailOutboxStatusCodes.Pending, cancellationToken);
        _db.EmailOutboxes.Add(new EmailOutbox
        {
            Id = Guid.NewGuid(),
            ToEmail = invitation.Email,
            ToUserId = null,
            Subject = "Reminder: Role invitation pending",
            BodyHtml = $"<p>Your invitation to join as {invitation.Role.NameEn} expires on {invitation.DeadlineAt:yyyy-MM-dd}.</p>",
            Category = "role_invitation_reminder",
            EmailOutboxStatusId = emailPendingStatus.Id,
            Attempts = 0,
        });
    }

    private async Task<Guid> StatusIdAsync(string code, CancellationToken cancellationToken) =>
        (await _db.RoleInvitationStatuses.SingleAsync(s => s.Code == code, cancellationToken)).Id;
}
