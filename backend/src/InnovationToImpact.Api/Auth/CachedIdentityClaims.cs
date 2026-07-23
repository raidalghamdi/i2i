namespace InnovationToImpact.Api.Auth;

public sealed record CachedIdentityClaims(
    Guid UserId,
    string SamAccountName,
    string Email,
    string? Department,
    IReadOnlyList<string> RoleCodes);
