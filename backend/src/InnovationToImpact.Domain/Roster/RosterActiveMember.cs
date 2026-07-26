namespace InnovationToImpact.Domain.Roster;

public sealed record RosterActiveMember(Guid UserId, string SamAccountName, string FullNameAr, string FullNameEn, string Email, bool IsActive);
