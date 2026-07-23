namespace InnovationToImpact.Domain.Entities;

public class FundingRequest
{
    public Guid Id { get; set; }

    public Guid IdeaId { get; set; }
    public Idea Idea { get; set; } = null!;

    public decimal AmountSar { get; set; }
    public string JustificationAr { get; set; } = string.Empty;
    public string JustificationEn { get; set; } = string.Empty;

    public Guid FundingStatusId { get; set; }
    public FundingStatus FundingStatus { get; set; } = null!;

    public decimal? ApprovedAmount { get; set; }

    public Guid? ApproverId { get; set; }
    public User? Approver { get; set; }

    public DateTime? DecidedAt { get; set; }
}
