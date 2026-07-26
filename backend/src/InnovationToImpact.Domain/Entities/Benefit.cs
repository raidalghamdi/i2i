namespace InnovationToImpact.Domain.Entities;

public class Benefit
{
    public Guid Id { get; set; }

    public Guid IdeaId { get; set; }
    public Idea Idea { get; set; } = null!;

    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }

    public Guid BenefitTypeId { get; set; }
    public BenefitType BenefitType { get; set; } = null!;

    public Guid BenefitCategoryId { get; set; }
    public BenefitCategory BenefitCategory { get; set; } = null!;

    public decimal? TargetValue { get; set; }
    public decimal? RealizedValue { get; set; }
    public string? MeasurementUnit { get; set; }
    public DateTime? MeasurementDate { get; set; }
    public string? EvidenceJson { get; set; }

    public Guid? VerifiedById { get; set; }
    public User? VerifiedBy { get; set; }
}
