namespace InnovationToImpact.Domain.Entities;

public class ComplianceControl
{
    public Guid Id { get; set; }

    public string ControlCode { get; set; } = string.Empty;

    public Guid StandardBodyId { get; set; }
    public StandardBody StandardBody { get; set; } = null!;

    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;

    public string? MappedFeaturePathsJson { get; set; }
    public string? EvidenceUrlsJson { get; set; }

    public Guid? OwnerId { get; set; }
    public User? Owner { get; set; }

    public Guid ComplianceControlStatusId { get; set; }
    public ComplianceControlStatus ComplianceControlStatus { get; set; } = null!;

    public DateTime? LastReviewedAt { get; set; }
}
