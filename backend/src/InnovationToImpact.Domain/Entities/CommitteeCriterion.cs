namespace InnovationToImpact.Domain.Entities;

public class CommitteeCriterion
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public decimal Weight { get; set; }
    public bool Active { get; set; } = true;
}
