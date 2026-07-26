namespace InnovationToImpact.Domain.Roster;

public sealed record RosterInviteRequest(string[] SamAccountNames, DateTime? DeadlineAt);
