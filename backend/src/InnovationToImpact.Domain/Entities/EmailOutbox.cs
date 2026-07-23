namespace InnovationToImpact.Domain.Entities;

public class EmailOutbox
{
    public Guid Id { get; set; }

    public string ToEmail { get; set; } = string.Empty;
    public Guid? ToUserId { get; set; }
    public User? ToUser { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public string? BodyText { get; set; }

    public string Category { get; set; } = string.Empty;

    public Guid EmailOutboxStatusId { get; set; }
    public EmailOutboxStatus EmailOutboxStatus { get; set; } = null!;

    public int Attempts { get; set; }
    public string? MetadataJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
