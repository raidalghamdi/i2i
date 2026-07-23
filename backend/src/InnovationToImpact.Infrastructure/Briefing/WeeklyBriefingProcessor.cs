using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Briefing;
using InnovationToImpact.Domain.Email;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Invitations;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InnovationToImpact.Infrastructure.Briefing;

public class WeeklyBriefingProcessor : IWeeklyBriefingProcessor
{
    private readonly InnovationDbContext _db;
    private readonly WeeklyBriefingOptions _options;

    public WeeklyBriefingProcessor(InnovationDbContext db, IOptions<WeeklyBriefingOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<WeeklyBriefingResult> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var windowStart = now.AddDays(-_options.WindowDays);

        var slaBreachesThisWeek = await _db.SlaTrackings
            .CountAsync(t => t.BreachedAt != null && t.BreachedAt >= windowStart && t.BreachedAt <= now, cancellationToken);

        var invitationsAcceptedThisWeek = await _db.TeamInvitations
            .CountAsync(i => i.AcceptedAt != null && i.AcceptedAt >= windowStart && i.AcceptedAt <= now, cancellationToken);

        var pendingInvitations = await _db.TeamInvitations
            .CountAsync(i => i.TeamInvitationStatus.Code == TeamInvitationStatusCodes.Pending, cancellationToken);

        var expiredInvitations = await _db.TeamInvitations
            .CountAsync(i => i.TeamInvitationStatus.Code == TeamInvitationStatusCodes.Expired, cancellationToken);

        var auditEntriesThisWeek = await _db.AuditLogs
            .CountAsync(a => a.OccurredAt >= windowStart && a.OccurredAt <= now, cancellationToken);

        var admins = await _db.Users
            .Where(u => u.UserRoles.Any(ur => ur.Role.Code == RoleCodes.Admin))
            .Select(u => new { u.Id, u.Email })
            .ToListAsync(cancellationToken);

        var emailPendingStatus = await _db.EmailOutboxStatuses.SingleAsync(s => s.Code == EmailOutboxStatusCodes.Pending, cancellationToken);

        var subject = "Weekly Briefing";
        var bodyHtml = $"<p>SLA breaches this week: {slaBreachesThisWeek}</p>" +
                       $"<p>Invitations accepted this week: {invitationsAcceptedThisWeek}</p>" +
                       $"<p>Pending invitations: {pendingInvitations}</p>" +
                       $"<p>Expired invitations: {expiredInvitations}</p>" +
                       $"<p>Audit log entries this week: {auditEntriesThisWeek}</p>";

        foreach (var admin in admins)
        {
            _db.EmailOutboxes.Add(new EmailOutbox
            {
                Id = Guid.NewGuid(),
                ToEmail = admin.Email,
                ToUserId = admin.Id,
                Subject = subject,
                BodyHtml = bodyHtml,
                Category = "weekly_briefing",
                EmailOutboxStatusId = emailPendingStatus.Id,
                Attempts = 0,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new WeeklyBriefingResult(
            slaBreachesThisWeek,
            invitationsAcceptedThisWeek,
            pendingInvitations,
            expiredInvitations,
            auditEntriesThisWeek,
            admins.Count);
    }
}
