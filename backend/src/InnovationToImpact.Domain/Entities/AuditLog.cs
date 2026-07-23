namespace InnovationToImpact.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }

    public long ChainSeq { get; set; }
    public string RowHash { get; set; } = string.Empty;
    public string? PrevHash { get; set; }

    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }

    public string Action { get; set; } = string.Empty;

    public Guid? ActorId { get; set; }
    public User? Actor { get; set; }

    public string? Payload { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
