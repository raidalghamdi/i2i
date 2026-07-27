using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Domain.Notifications;

namespace InnovationToImpact.Infrastructure.Ideas;

// Change 20260726
public class IdeaStatusNotifier : IIdeaStatusNotifier
{
    private readonly INotificationService _notificationService;

    public IdeaStatusNotifier(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public Task NotifyStatusChangedAsync(Idea idea, string newStatusCode, CancellationToken cancellationToken = default) =>
        _notificationService.CreateAndPublishAsync(
            idea.SubmitterId, NotificationTypes.IdeaStatusChanged,
            "تحديث حالة الفكرة", "Idea status updated",
            $"أصبحت حالة الفكرة \"{idea.TitleAr}\" الآن: {newStatusCode}.",
            $"The idea \"{idea.TitleEn}\" is now: {newStatusCode}.",
            $"/ideas/{idea.Id}", null, cancellationToken);
}
