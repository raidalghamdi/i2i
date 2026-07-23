using InnovationToImpact.Domain.Email;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Invitations;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InnovationToImpact.Infrastructure.Invitations;

public class InvitationReminderProcessor : IInvitationReminderProcessor
{
    private readonly InnovationDbContext _db;
    private readonly InvitationReminderOptions _options;

    public InvitationReminderProcessor(InnovationDbContext db, IOptions<InvitationReminderOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<InvitationReminderResult> ProcessAsync(CancellationToken cancellationToken = default)
    {
        var pendingStatus = await _db.TeamInvitationStatuses.SingleAsync(s => s.Code == TeamInvitationStatusCodes.Pending, cancellationToken);
        var expiredStatus = await _db.TeamInvitationStatuses.SingleAsync(s => s.Code == TeamInvitationStatusCodes.Expired, cancellationToken);
        var emailPendingStatus = await _db.EmailOutboxStatuses.SingleAsync(s => s.Code == EmailOutboxStatusCodes.Pending, cancellationToken);

        var pendingInvitations = await _db.TeamInvitations
            .Include(i => i.Team)
            .Where(i => i.TeamInvitationStatusId == pendingStatus.Id)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var expiredCount = 0;
        var remindersQueued = 0;

        foreach (var invitation in pendingInvitations)
        {
            if (invitation.ExpiresAt <= now)
            {
                invitation.TeamInvitationStatusId = expiredStatus.Id;
                expiredCount++;
            }
            else if (invitation.ExpiresAt - now <= TimeSpan.FromDays(_options.ReminderWindowDays))
            {
                _db.EmailOutboxes.Add(new EmailOutbox
                {
                    Id = Guid.NewGuid(),
                    ToEmail = invitation.InvitedEmail,
                    ToUserId = null,
                    Subject = $"Reminder: Invitation to join {invitation.Team.NameEn}",
                    BodyHtml = $"<p>Your invitation to join \"{invitation.Team.NameEn}\" expires on {invitation.ExpiresAt:yyyy-MM-dd}. Invitation token: {invitation.Token}</p>",
                    Category = "team_invitation_reminder",
                    EmailOutboxStatusId = emailPendingStatus.Id,
                    Attempts = 0,
                });
                remindersQueued++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new InvitationReminderResult(pendingInvitations.Count, expiredCount, remindersQueued);
    }
}
