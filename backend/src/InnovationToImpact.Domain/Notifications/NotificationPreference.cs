namespace InnovationToImpact.Domain.Notifications;

// Change 20260726
public class NotificationPreference
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string CategoryKey { get; set; } = string.Empty;

    public bool Muted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
