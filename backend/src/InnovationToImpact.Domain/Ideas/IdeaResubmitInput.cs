namespace InnovationToImpact.Domain.Ideas;

public sealed record IdeaResubmitInput(
    string TitleAr,
    string TitleEn,
    string ProblemStatementAr,
    string ProblemStatementEn,
    string ProposedSolutionAr,
    string ProposedSolutionEn,
    string ExpectedBenefitsAr,
    string ExpectedBenefitsEn,
    Guid ActivityId,
    Guid StrategicThemeId,
    Guid? ChallengeId,
    string ParticipationType,
    string? TeamName,
    IReadOnlyList<TeamMemberInput> TeamMembers);
