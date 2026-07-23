namespace InnovationToImpact.Domain.Entities;

public class Implementation
{
    public Guid Id { get; set; }

    public Guid IdeaId { get; set; }
    public Idea Idea { get; set; } = null!;

    public Guid OperationalOwnerId { get; set; }
    public User OperationalOwner { get; set; } = null!;

    public string IntegrationPlanAr { get; set; } = string.Empty;
    public string IntegrationPlanEn { get; set; } = string.Empty;
    public string ResourceCommitmentAr { get; set; } = string.Empty;
    public string ResourceCommitmentEn { get; set; } = string.Empty;

    public Guid HandoverStatusId { get; set; }
    public HandoverStatus HandoverStatus { get; set; } = null!;

    public string? LineUnit { get; set; }
}
