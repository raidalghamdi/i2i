using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Notifications;

public interface INotificationPublisher
{
    Task PublishAsync(Guid userId, Notification notification, CancellationToken cancellationToken = default);
}
