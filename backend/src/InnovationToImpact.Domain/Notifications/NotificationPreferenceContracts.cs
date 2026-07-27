namespace InnovationToImpact.Domain.Notifications;

// Change 20260726
public sealed class NotificationPreferenceItem
{
    public string? CategoryKey { get; set; }
    public bool Muted { get; set; }
}

// Change 20260726
public sealed class NotificationPreferencesUpdateRequest
{
    public List<NotificationPreferenceItem>? Preferences { get; set; }
}
