namespace InnovationToImpact.Domain.Roster;

public sealed record RoleInvitationCreateInput(string SamAccountName, string RoleCode, DateTime? DeadlineAt, string Source);
