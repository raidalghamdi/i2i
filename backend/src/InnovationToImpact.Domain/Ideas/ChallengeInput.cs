namespace InnovationToImpact.Domain.Ideas;

public sealed record ChallengeInput(Guid StrategicThemeId, string TextAr, string TextEn, int SortOrder, bool IsActive);
