namespace InnovationToImpact.Domain.Entities;

public class Escalation
{
    public Guid Id { get; set; }

    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }

    public Guid EscalationTierId { get; set; }
    public EscalationTier EscalationTier { get; set; } = null!;

    public string ReasonAr { get; set; } = string.Empty;
    public string ReasonEn { get; set; } = string.Empty;
    public string? ResolutionAr { get; set; }
    public string? ResolutionEn { get; set; }

    public Guid EscalationStatusId { get; set; }
    public EscalationStatus EscalationStatus { get; set; } = null!;

    public Guid? OwnerId { get; set; }
    public User? Owner { get; set; }

    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
}
