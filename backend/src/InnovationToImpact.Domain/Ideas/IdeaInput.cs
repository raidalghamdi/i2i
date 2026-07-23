namespace InnovationToImpact.Domain.Ideas;

public sealed record IdeaInput(
    string TitleAr,
    string TitleEn,
    string ProblemStatementAr,
    string ProblemStatementEn,
    string ProposedSolutionAr,
    string ProposedSolutionEn,
    string ExpectedBenefitsAr,
    string ExpectedBenefitsEn,
    Guid StrategicThemeId,
    Guid ActivityId,
    Guid? ChallengeId,
    string ParticipationType,
    string? TeamName,
    IReadOnlyList<TeamMemberInput> TeamMembers,
    bool IpAcknowledged,
    bool TermsAgreed);

public sealed record TeamMemberInput(string Name, string Email);
