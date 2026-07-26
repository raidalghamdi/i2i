using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Notifications;

public interface INotificationService
{
    Task<Notification> CreateAndPublishAsync(
        Guid userId,
        string notificationType,
        string titleAr,
        string titleEn,
        string bodyAr,
        string bodyEn,
        string? link,
        string? payloadJson,
        CancellationToken cancellationToken = default);
}
