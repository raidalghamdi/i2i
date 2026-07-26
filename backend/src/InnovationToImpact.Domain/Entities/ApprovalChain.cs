namespace InnovationToImpact.Domain.Entities;

public class ApprovalChain
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
