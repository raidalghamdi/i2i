namespace InnovationToImpact.Domain.Entities;

public class ApprovalInstance
{
    public Guid Id { get; set; }

    public Guid ApprovalChainId { get; set; }
    public ApprovalChain ApprovalChain { get; set; } = null!;

    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }

    public Guid ApprovalInstanceStatusId { get; set; }
    public ApprovalInstanceStatus ApprovalInstanceStatus { get; set; } = null!;

    public int CurrentStepOrder { get; set; } = 1;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
