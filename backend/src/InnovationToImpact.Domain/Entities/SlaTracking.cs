namespace InnovationToImpact.Domain.Entities;

public class SlaTracking
{
    public Guid Id { get; set; }

    public Guid SlaPolicyId { get; set; }
    public SlaPolicy SlaPolicy { get; set; } = null!;

    public Guid EntityId { get; set; }

    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime TargetAt { get; set; }
    public DateTime? BreachedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
