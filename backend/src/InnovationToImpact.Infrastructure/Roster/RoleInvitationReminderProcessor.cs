using InnovationToImpact.Domain.Email;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Roster;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Roster;

public class RoleInvitationReminderProcessor : IRoleInvitationReminderProcessor
{
    private readonly InnovationDbContext _db;

    public RoleInvitationReminderProcessor(InnovationDbContext db) { _db = db; }

    public async Task<RoleInvitationReminderResult> ProcessAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _db.RoleInvitationSettings.SingleAsync(cancellationToken);

        var pendingStatus = await _db.RoleInvitationStatuses.SingleAsync(s => s.Code == RoleInvitationStatusCodes.Pending, cancellationToken);
        var expiredStatus = await _db.RoleInvitationStatuses.SingleAsync(s => s.Code == RoleInvitationStatusCodes.Expired, cancellationToken);
        var emailPendingStatus = await _db.EmailOutboxStatuses.SingleAsync(s => s.Code == EmailOutboxStatusCodes.Pending, cancellationToken);

        var pending = await _db.RoleInvitations
            .Include(ri => ri.Role)
            .Where(ri => ri.RoleInvitationStatusId == pendingStatus.Id)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var expiredCount = 0;
        var remindersQueued = 0;

        foreach (var invitation in pending)
        {
            if (invitation.DeadlineAt is not null && invitation.DeadlineAt <= now)
            {
                var pendingGrant = await _db.PendingRoleGrants.SingleOrDefaultAsync(
                    g => g.SamAccountName == invitation.SamAccountName && g.RoleId == invitation.RoleId,
                    cancellationToken);
                if (pendingGrant is not null) _db.PendingRoleGrants.Remove(pendingGrant);

                invitation.RoleInvitationStatusId = expiredStatus.Id;
                invitation.UpdatedAt = now;
                expiredCount++;
                continue;
            }

            var dueForReminder = settings.Enabled
                && invitation.ReminderCount < settings.MaxReminders
                && (invitation.LastReminderAt is null || now - invitation.LastReminderAt >= TimeSpan.FromHours(settings.ReminderGapHours));

            if (dueForReminder && !string.IsNullOrEmpty(invitation.Email))
            {
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
                invitation.ReminderCount++;
                invitation.LastReminderAt = now;
                invitation.UpdatedAt = now;
                remindersQueued++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new RoleInvitationReminderResult(pending.Count, expiredCount, remindersQueued);
    }
}
