using System.Text.Json;
using InnovationToImpact.Domain.Email;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Notifications;
using InnovationToImpact.Domain.Phases;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Phases;

public class PhaseScheduleService : IPhaseScheduleService
{
    private readonly InnovationDbContext _db;
    private readonly INotificationService _notificationService;

    public PhaseScheduleService(InnovationDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<IReadOnlyList<PhaseSchedule>> ListAsync(CancellationToken cancellationToken = default) =>
        await _db.PhaseSchedules.OrderBy(p => p.Idx).ToListAsync(cancellationToken);

    public async Task<PhaseScheduleCommandResult> UpdateAsync(int idx, DateTime? startsAt, DateTime? endsAt, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        var phase = await _db.PhaseSchedules.SingleOrDefaultAsync(p => p.Idx == idx, cancellationToken);
        if (phase is null) return new PhaseScheduleCommandResult(PhaseScheduleCommandStatus.NotFound);

        phase.StartsAt = startsAt;
        phase.EndsAt = endsAt;
        phase.UpdatedAt = DateTime.UtcNow;
        phase.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(cancellationToken);
        return new PhaseScheduleCommandResult(PhaseScheduleCommandStatus.Success, phase);
    }

    public async Task<PhaseAnnounceResult> AnnounceAsync(int idx, Guid actorId, CancellationToken cancellationToken = default)
    {
        var phase = await _db.PhaseSchedules.SingleOrDefaultAsync(p => p.Idx == idx, cancellationToken);
        if (phase is null) return new PhaseAnnounceResult(PhaseAnnounceStatus.NotFound, 0);

        var audienceRoleCodes = await _db.PhaseAudiences
            .Where(a => a.PhaseIdx == idx)
            .Select(a => a.RoleCode)
            .Distinct()
            .ToListAsync(cancellationToken);

        var recipientIds = await _db.Users
            .Where(u => u.IsActive && u.UserRoles.Any(ur => audienceRoleCodes.Contains(ur.Role.Code)))
            .Select(u => u.Id)
            .Distinct()
            .ToListAsync(cancellationToken);

        var titleAr = $"تم الإعلان عن مرحلة: {phase.LabelAr}";
        var titleEn = $"Phase announced: {phase.LabelEn}";
        var bodyAr = $"تم الإعلان عن بدء مرحلة \"{phase.LabelAr}\". يرجى مراجعة لوحة التحكم للاطلاع على التفاصيل.";
        var bodyEn = $"The phase \"{phase.LabelEn}\" has been announced. Please check your dashboard for details.";
        var link = "/dashboard";
        var payloadJson = JsonSerializer.Serialize(new { phaseIdx = phase.Idx, phaseCode = phase.Code });

        if (recipientIds.Count > 0)
        {
            var emailPendingStatus = await _db.EmailOutboxStatuses.SingleAsync(s => s.Code == EmailOutboxStatusCodes.Pending, cancellationToken);
            var recipients = await _db.Users.Where(u => recipientIds.Contains(u.Id)).ToListAsync(cancellationToken);

            foreach (var recipient in recipients)
            {
                await _notificationService.CreateAndPublishAsync(
                    recipient.Id,
                    NotificationTypes.PhaseAnnounced,
                    titleAr,
                    titleEn,
                    bodyAr,
                    bodyEn,
                    link,
                    payloadJson,
                    cancellationToken);

                _db.EmailOutboxes.Add(new EmailOutbox
                {
                    Id = Guid.NewGuid(),
                    ToEmail = recipient.Email,
                    ToUserId = recipient.Id,
                    Subject = titleEn,
                    BodyHtml = $"<p>{bodyEn}</p>",
                    Category = NotificationTypes.PhaseAnnounced,
                    EmailOutboxStatusId = emailPendingStatus.Id,
                    Attempts = 0,
                });
            }
        }

        phase.AnnouncedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new PhaseAnnounceResult(PhaseAnnounceStatus.Success, recipientIds.Count);
    }

    public async Task<IReadOnlyList<string>> GetAudienceAsync(int idx, CancellationToken cancellationToken = default) =>
        await _db.PhaseAudiences
            .Where(a => a.PhaseIdx == idx)
            .Select(a => a.RoleCode)
            .Distinct()
            .ToListAsync(cancellationToken);

    public async Task SetAudienceAsync(int idx, IReadOnlyList<string> roleCodes, CancellationToken cancellationToken = default)
    {
        var knownRoleCodes = await _db.Roles.Select(r => r.Code).ToListAsync(cancellationToken);
        var validRoleCodes = roleCodes
            .Where(c => knownRoleCodes.Contains(c))
            .Distinct()
            .ToList();

        var existing = await _db.PhaseAudiences.Where(a => a.PhaseIdx == idx).ToListAsync(cancellationToken);
        _db.PhaseAudiences.RemoveRange(existing);

        foreach (var roleCode in validRoleCodes)
        {
            _db.PhaseAudiences.Add(new PhaseAudience
            {
                Id = Guid.NewGuid(),
                PhaseIdx = idx,
                RoleCode = roleCode,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
