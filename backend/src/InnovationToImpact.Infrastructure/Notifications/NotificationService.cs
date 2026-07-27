using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Notifications;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Notifications;

public class NotificationService : INotificationService
{
    private readonly InnovationDbContext _db;
    private readonly INotificationPublisher _publisher;

    public NotificationService(InnovationDbContext db, INotificationPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task<Notification?> CreateAndPublishAsync(
        Guid userId,
        string notificationType,
        string titleAr,
        string titleEn,
        string bodyAr,
        string bodyEn,
        string? link,
        string? payloadJson,
        CancellationToken cancellationToken = default)
    {
        // Change 20260726 — the mute gate lives here rather than at the trigger sites so every
        // existing and future trigger inherits it. A missing preference row means un-muted.
        var muted = await _db.NotificationPreferences.AnyAsync(
            p => p.UserId == userId && p.CategoryKey == notificationType && p.Muted,
            cancellationToken);
        if (muted) return null;

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            NotificationType = notificationType,
            TitleAr = titleAr,
            TitleEn = titleEn,
            BodyAr = bodyAr,
            BodyEn = bodyEn,
            Link = link,
            PayloadJson = payloadJson,
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(cancellationToken);

        await _publisher.PublishAsync(userId, notification, cancellationToken);

        return notification;
    }

    // Change 20260726
    public async Task<int> CreateAndPublishToUsersAsync(
        IEnumerable<Guid> userIds,
        string notificationType,
        string titleAr,
        string titleEn,
        string bodyAr,
        string bodyEn,
        string? link,
        string? payloadJson,
        CancellationToken cancellationToken = default)
    {
        var sent = 0;
        foreach (var userId in userIds.Distinct())
        {
            var notification = await CreateAndPublishAsync(
                userId, notificationType, titleAr, titleEn, bodyAr, bodyEn, link, payloadJson, cancellationToken);
            if (notification is not null) sent++;
        }

        return sent;
    }

    // Change 20260726
    public async Task<int> CreateAndPublishToRolesAsync(
        IEnumerable<string> roleCodes,
        string notificationType,
        string titleAr,
        string titleEn,
        string bodyAr,
        string bodyEn,
        string? link,
        string? payloadJson,
        CancellationToken cancellationToken = default)
    {
        var codes = roleCodes.Distinct().ToList();
        if (codes.Count == 0) return 0;

        var userIds = await _db.Users
            .Where(u => u.UserRoles.Any(ur => codes.Contains(ur.Role.Code)))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        return await CreateAndPublishToUsersAsync(
            userIds, notificationType, titleAr, titleEn, bodyAr, bodyEn, link, payloadJson, cancellationToken);
    }
}
