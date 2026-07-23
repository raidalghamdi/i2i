namespace InnovationToImpact.Domain.Entities;

public class Idea
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string ProblemStatementAr { get; set; } = string.Empty;
    public string ProblemStatementEn { get; set; } = string.Empty;
    public string ProposedSolutionAr { get; set; } = string.Empty;
    public string ProposedSolutionEn { get; set; } = string.Empty;
    public string ExpectedBenefitsAr { get; set; } = string.Empty;
    public string ExpectedBenefitsEn { get; set; } = string.Empty;

    public Guid StrategicThemeId { get; set; }
    public StrategicTheme StrategicTheme { get; set; } = null!;

    public Guid? ActivityId { get; set; }
    public Activity? Activity { get; set; }

    public Guid IdeaStatusId { get; set; }
    public IdeaStatus IdeaStatus { get; set; } = null!;

    public int CurrentStage { get; set; }

    public Guid SubmitterId { get; set; }
    public User Submitter { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public decimal? CommitteeFinalScore { get; set; }
    public int? FinalRank { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? EnteredEvaluationAt { get; set; }
    public string? ScreeningReason { get; set; }
    public Guid? ChallengeId { get; set; }
    public Challenge? Challenge { get; set; }
    public string ParticipationType { get; set; } = "individual";
    public string? TeamName { get; set; }
    public bool IpAcknowledged { get; set; }
    public bool TermsAgreed { get; set; }
    public string? EditableSections { get; set; }
    public ICollection<IdeaTeamMember> TeamMembers { get; set; } = new List<IdeaTeamMember>();
}
