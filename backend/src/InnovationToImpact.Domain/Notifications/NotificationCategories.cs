namespace InnovationToImpact.Domain.Notifications;

// Change 20260726
public sealed record NotificationCategory(string Key, string LabelAr, string LabelEn);

public static class NotificationCategories
{
    public static readonly IReadOnlyList<NotificationCategory> All = new[]
    {
        new NotificationCategory(NotificationTypes.PhaseAnnounced, "إعلان مرحلة", "Phase Announced"),
        new NotificationCategory(NotificationTypes.EvaluationAssigned, "تعيين تقييم", "Evaluation Assigned"),
        new NotificationCategory(NotificationTypes.ApprovalRequested, "طلب موافقة", "Approval Requested"),
        new NotificationCategory(NotificationTypes.IdeaWithdrawn, "سحب فكرة", "Idea Withdrawn"),
        new NotificationCategory(NotificationTypes.IdeaSubmitted, "تقديم فكرة", "Idea Submitted"),
        new NotificationCategory(NotificationTypes.EvaluationCompleted, "اكتمال التقييم", "Evaluation Completed"),
        new NotificationCategory(NotificationTypes.CommitteeDecisionMade, "قرار اللجنة", "Committee Decision Made"),
        new NotificationCategory(NotificationTypes.IdeaStatusChanged, "تغيير حالة الفكرة", "Idea Status Changed"),
    };

    public static bool IsKnown(string key) => All.Any(c => c.Key == key);
}
