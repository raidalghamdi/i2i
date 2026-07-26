namespace InnovationToImpact.Domain.Entities;

public class EmailLog
{
    public Guid Id { get; set; }

    public string Provider { get; set; } = string.Empty;

    public Guid EmailLogStatusId { get; set; }
    public EmailLogStatus EmailLogStatus { get; set; } = null!;

    public string? ProviderMessageId { get; set; }
    public bool RedirectApplied { get; set; }

    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }

    public string ToEmail { get; set; } = string.Empty;
    public Guid? ToUserId { get; set; }
    public User? ToUser { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
