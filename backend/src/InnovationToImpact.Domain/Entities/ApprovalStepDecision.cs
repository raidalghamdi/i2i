namespace InnovationToImpact.Domain.Entities;

public class ApprovalStepDecision
{
    public Guid Id { get; set; }

    public Guid ApprovalInstanceId { get; set; }
    public ApprovalInstance ApprovalInstance { get; set; } = null!;

    public Guid ApprovalChainStepId { get; set; }
    public ApprovalChainStep ApprovalChainStep { get; set; } = null!;

    public Guid DeciderId { get; set; }
    public User Decider { get; set; } = null!;

    public Guid ApprovalDecisionTypeId { get; set; }
    public ApprovalDecisionType ApprovalDecisionType { get; set; } = null!;

    public string? CommentsAr { get; set; }
    public string? CommentsEn { get; set; }

    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
}
