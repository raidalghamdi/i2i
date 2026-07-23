namespace InnovationToImpact.Domain.Entities;

public class CommitteeDecision
{
    public Guid Id { get; set; }

    public Guid IdeaId { get; set; }
    public Idea Idea { get; set; } = null!;

    public string CommitteeName { get; set; } = string.Empty;

    public Guid CommitteeDecisionTypeId { get; set; }
    public CommitteeDecisionType CommitteeDecisionType { get; set; } = null!;

    public string CriteriaScoresJson { get; set; } = "{}";
    public decimal TotalScore { get; set; }

    public bool QuorumMet { get; set; }
    public string? Comments { get; set; }
    public string? AttachmentsJson { get; set; }
    public DateTime? DecidedAt { get; set; }

    public Guid? DecidedById { get; set; }
    public User? DecidedBy { get; set; }
}
