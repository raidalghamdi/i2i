namespace InnovationToImpact.Domain.Entities;

public class EscalationEvent
{
    public Guid Id { get; set; }

    public Guid EscalationId { get; set; }
    public Escalation Escalation { get; set; } = null!;

    public string EventType { get; set; } = string.Empty;

    public Guid? FromTierId { get; set; }
    public EscalationTier? FromTier { get; set; }

    public Guid? ToTierId { get; set; }
    public EscalationTier? ToTier { get; set; }

    public Guid? ActorId { get; set; }
    public User? Actor { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
