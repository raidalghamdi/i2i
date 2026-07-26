namespace InnovationToImpact.Domain.Phases;

public sealed record PhaseScheduleInput(DateTime? StartsAt, DateTime? EndsAt);

public sealed record PhaseAudienceInput(string[] RoleCodes);
