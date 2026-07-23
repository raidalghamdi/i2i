using System.Security.Claims;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InnovationToImpact.Infrastructure.Auth;

/// <summary>
/// Issues/rotates JWT access + refresh token pairs for the Staging (cloud, pre-AD-integration) auth
/// path. Access tokens carry the same claim shapes IdentityClaimsTransformation would otherwise
/// produce (NameIdentifier/Email/department/Role) so every existing [Authorize] policy keeps working
/// unchanged regardless of which auth path a request came through.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly InnovationDbContext _db;
    private readonly JwtOptions _options;

    public JwtTokenService(InnovationDbContext db, IOptions<JwtOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<IssuedTokens> IssueAsync(User user, IReadOnlyList<string> roleCodes, string? clientIp, CancellationToken cancellationToken = default)
    {
        var (accessToken, expiresAt) = CreateAccessToken(user, roleCodes);
        var refreshToken = await CreateAndStoreRefreshTokenAsync(user.Id, clientIp, cancellationToken);
        return new IssuedTokens(accessToken, refreshToken, expiresAt);
    }

    public async Task<IssuedTokens?> RotateAsync(string presentedRefreshToken, string? clientIp, CancellationToken cancellationToken = default)
    {
        var hash = Hash(presentedRefreshToken);
        var existing = await _db.RefreshTokens
            .Include(t => t.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (existing is null || !existing.IsActive)
        {
            return null;
        }

        existing.RevokedAt = DateTime.UtcNow;

        var user = existing.User;
        var roleCodes = user.IsActive ? user.UserRoles.Where(ur => ur.Role.IsActive).Select(ur => ur.Role.Code).ToList() : new List<string>();
        var (accessToken, expiresAt) = CreateAccessToken(user, roleCodes);
        var newRefreshToken = await CreateAndStoreRefreshTokenAsync(user.Id, clientIp, cancellationToken, replaces: existing);

        await _db.SaveChangesAsync(cancellationToken);
        return new IssuedTokens(accessToken, newRefreshToken, expiresAt);
    }

    public async Task RevokeAsync(string presentedRefreshToken, CancellationToken cancellationToken = default)
    {
        var hash = Hash(presentedRefreshToken);
        var existing = await _db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (existing is not null && existing.RevokedAt is null)
        {
            existing.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private (string Token, DateTime ExpiresAt) CreateAccessToken(User user, IReadOnlyList<string> roleCodes)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.SamAccountName),
            new(ClaimTypes.Email, user.Email),
        };
        if (!string.IsNullOrEmpty(user.Department))
        {
            claims.Add(new Claim("department", user.Department));
        }
        claims.AddRange(roleCodes.Select(code => new Claim(ClaimTypes.Role, code)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private async Task<string> CreateAndStoreRefreshTokenAsync(Guid userId, string? clientIp, CancellationToken cancellationToken, RefreshToken? replaces = null)
    {
        var plaintext = GenerateOpaqueToken();
        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Hash(plaintext),
            ExpiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenDays),
            CreatedByIp = clientIp,
        };
        _db.RefreshTokens.Add(entity);

        if (replaces is not null)
        {
            replaces.ReplacedByTokenId = entity.Id;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return plaintext;
    }

    private static string GenerateOpaqueToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
