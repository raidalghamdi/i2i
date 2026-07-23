namespace InnovationToImpact.Domain.Entities;

public class ScaleDecision
{
    public Guid Id { get; set; }

    public Guid IdeaId { get; set; }
    public Idea Idea { get; set; } = null!;

    public string EvidenceOfViabilityAr { get; set; } = string.Empty;
    public string EvidenceOfViabilityEn { get; set; } = string.Empty;
    public string ValueAssessmentAr { get; set; } = string.Empty;
    public string ValueAssessmentEn { get; set; } = string.Empty;
    public string RiskAssessmentAr { get; set; } = string.Empty;
    public string RiskAssessmentEn { get; set; } = string.Empty;

    public decimal StrategicFitScore { get; set; }

    public Guid ScaleDecisionTypeId { get; set; }
    public ScaleDecisionType ScaleDecisionType { get; set; } = null!;

    public Guid? DecidedById { get; set; }
    public User? DecidedBy { get; set; }
}
