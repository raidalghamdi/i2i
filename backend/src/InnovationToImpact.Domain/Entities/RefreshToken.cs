namespace InnovationToImpact.Domain.Entities;

/// <summary>
/// Opaque refresh token for the JWT login path (Staging/cloud deployments only).
/// Only the SHA-256 hash of the token is ever persisted -- the plaintext value is returned to the
/// client once at issuance and never stored, the same principle used for password hashes.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedByIp { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;
}
