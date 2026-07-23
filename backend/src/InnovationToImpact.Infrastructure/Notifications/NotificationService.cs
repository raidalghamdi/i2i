using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Notifications;
using InnovationToImpact.Infrastructure.Data;

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

    public async Task<Notification> CreateAndPublishAsync(
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
}
