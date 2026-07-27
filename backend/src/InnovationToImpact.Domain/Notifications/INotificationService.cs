using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Notifications;

public interface INotificationService
{
    // Change 20260726 — nullable: returns null when the recipient has muted the category.
    Task<Notification?> CreateAndPublishAsync(
        Guid userId,
        string notificationType,
        string titleAr,
        string titleEn,
        string bodyAr,
        string bodyEn,
        string? link,
        string? payloadJson,
        CancellationToken cancellationToken = default);

    // Change 20260726 — notifications are per-user, so any audience wider than one recipient has to
    // fan out. These helpers centralize the loop and de-duplication every trigger site would
    // otherwise repeat, and return the number of recipients actually notified.
    Task<int> CreateAndPublishToUsersAsync(
        IEnumerable<Guid> userIds,
        string notificationType,
        string titleAr,
        string titleEn,
        string bodyAr,
        string bodyEn,
        string? link,
        string? payloadJson,
        CancellationToken cancellationToken = default);

    Task<int> CreateAndPublishToRolesAsync(
        IEnumerable<string> roleCodes,
        string notificationType,
        string titleAr,
        string titleEn,
        string bodyAr,
        string bodyEn,
        string? link,
        string? payloadJson,
        CancellationToken cancellationToken = default);
}
