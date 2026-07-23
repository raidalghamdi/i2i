namespace InnovationToImpact.Domain.Entities;

public class ApprovalChainStep
{
    public Guid Id { get; set; }

    public Guid ApprovalChainId { get; set; }
    public ApprovalChain ApprovalChain { get; set; } = null!;

    public int StepOrder { get; set; }

    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public bool IsRequired { get; set; } = true;

    public int MinApprovers { get; set; } = 1;
    public string LabelAr { get; set; } = string.Empty;
    public string LabelEn { get; set; } = string.Empty;
}
