namespace InnovationToImpact.Domain.Auth;

public sealed record AdIdentity(
    string SamAccountName,
    string DisplayName,
    string Email,
    string? Department,
    string? Title,
    string? ManagerEmail);
