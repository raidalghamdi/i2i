namespace InnovationToImpact.Domain.Roster;

public sealed record RosterHubRow(string RoleCode, string RoleNameAr, string RoleNameEn, int ActiveCount, int PendingCount, int ExpiredCount, int WithdrawnCount);
