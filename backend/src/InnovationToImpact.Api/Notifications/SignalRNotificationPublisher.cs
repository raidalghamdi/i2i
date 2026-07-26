using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace InnovationToImpact.Api.Notifications;

public class SignalRNotificationPublisher : INotificationPublisher
{
    private readonly IHubContext<NotificationsHub> _hubContext;

    public SignalRNotificationPublisher(IHubContext<NotificationsHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PublishAsync(Guid userId, Notification notification, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients.User(userId.ToString()).SendAsync(
            "ReceiveNotification",
            new
            {
                id = notification.Id,
                notificationType = notification.NotificationType,
                titleAr = notification.TitleAr,
                titleEn = notification.TitleEn,
                bodyAr = notification.BodyAr,
                bodyEn = notification.BodyEn,
                link = notification.Link,
            },
            cancellationToken);
    }
}
