namespace InnovationToImpact.Domain.Ideas;

public sealed record IdeaResubmitInput(
    string TitleAr,
    string TitleEn,
    string ProposedSolutionAr,
    string ProposedSolutionEn,
    Guid ActivityId,
    Guid StrategicThemeId,
    Guid? ChallengeId,
    string ParticipationType,
    string? TeamName,
    IReadOnlyList<TeamMemberInput> TeamMembers);
