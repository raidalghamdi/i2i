using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Auth;

public record IssuedTokens(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt);

public interface IJwtTokenService
{
    /// <summary>Issues a fresh access + refresh token pair for an authenticated user and persists the refresh token's hash.</summary>
    Task<IssuedTokens> IssueAsync(User user, IReadOnlyList<string> roleCodes, string? clientIp, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a presented refresh token, revokes it, and issues a new pair (rotation).
    /// Returns null if the token is unknown, expired, or already revoked.
    /// </summary>
    Task<IssuedTokens?> RotateAsync(string presentedRefreshToken, string? clientIp, CancellationToken cancellationToken = default);

    /// <summary>Revokes a refresh token so it can no longer be used, e.g. on logout.</summary>
    Task RevokeAsync(string presentedRefreshToken, CancellationToken cancellationToken = default);
}
