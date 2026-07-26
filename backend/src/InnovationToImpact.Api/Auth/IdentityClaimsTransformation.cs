using System.Security.Claims;
using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Auth;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace InnovationToImpact.Api.Auth;

public class IdentityClaimsTransformation : IClaimsTransformation
{
    private const string SyncedClaimType = "identity-sync-complete";
    public const string SamAccountNameClaimType = "samAccountName";

    private readonly InnovationDbContext _db;
    private readonly IAdIdentityLookupService _lookupService;
    private readonly IMemoryCache _cache;
    private readonly ActiveDirectoryOptions _options;

    public IdentityClaimsTransformation(
        InnovationDbContext db,
        IAdIdentityLookupService lookupService,
        IMemoryCache cache,
        IOptions<ActiveDirectoryOptions> options)
    {
        _db = db;
        _lookupService = lookupService;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || string.IsNullOrEmpty(identity.Name))
        {
            return principal;
        }

        if (identity.HasClaim(c => c.Type == SyncedClaimType))
        {
            return principal;
        }

        var samAccountName = ExtractSamAccountName(identity.Name);
        var cacheKey = $"identity-claims:{samAccountName}";

        if (!_cache.TryGetValue(cacheKey, out CachedIdentityClaims? cached) || cached is null)
        {
            cached = await ResolveAndSyncAsync(samAccountName);
            _cache.Set(cacheKey, cached, TimeSpan.FromMinutes(_options.CacheTtlMinutes));
        }

        identity.AddClaim(new Claim(SyncedClaimType, "1"));
        identity.AddClaim(new Claim(ClaimTypes.Email, cached.Email));
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, cached.UserId.ToString()));
        // The identity's Name is domain-qualified under Negotiate (DOMAIN\sam or a UPN); expose the bare
        // SAM as its own claim so callers can match it against stored IdeaTeamMember.SamAccountName values.
        identity.AddClaim(new Claim(SamAccountNameClaimType, samAccountName));
        if (!string.IsNullOrEmpty(cached.Department))
        {
            identity.AddClaim(new Claim("department", cached.Department));
        }
        foreach (var roleCode in cached.RoleCodes)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, roleCode));
        }

        return principal;
    }

    private async Task<CachedIdentityClaims> ResolveAndSyncAsync(string samAccountName)
    {
        AdIdentity? adIdentity;
        try
        {
            adIdentity = await _lookupService.ResolveAsync(samAccountName);
        }
        catch (Exception ex) when (ex is not IdentityResolutionUnavailableException)
        {
            throw new IdentityResolutionUnavailableException(samAccountName, ex);
        }

        if (adIdentity is null)
        {
            throw new IdentityResolutionUnavailableException(samAccountName);
        }

        var user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.SamAccountName == samAccountName);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                SamAccountName = samAccountName,
                Email = adIdentity.Email,
                FullNameAr = adIdentity.DisplayName,
                FullNameEn = adIdentity.DisplayName,
                Department = adIdentity.Department,
                ManagerEmail = adIdentity.ManagerEmail,
                Title = adIdentity.Title
            };
            _db.Users.Add(user);

            var pendingGrants = await _db.PendingRoleGrants
                .Include(g => g.Role)
                .Where(g => g.SamAccountName == samAccountName)
                .ToListAsync();
            foreach (var grant in pendingGrants)
            {
                user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = grant.RoleId, Role = grant.Role, IsPrimary = false });
            }
            _db.PendingRoleGrants.RemoveRange(pendingGrants);

            if (pendingGrants.Count > 0)
            {
                var roleIds = pendingGrants.Select(g => g.RoleId).ToList();
                var pendingInvitationStatusId = await _db.RoleInvitationStatuses
                    .Where(s => s.Code == InnovationToImpact.Domain.Roster.RoleInvitationStatusCodes.Pending)
                    .Select(s => s.Id)
                    .SingleAsync();
                var matchingInvitations = await _db.RoleInvitations
                    .Where(ri => ri.SamAccountName == samAccountName && roleIds.Contains(ri.RoleId) && ri.RoleInvitationStatusId == pendingInvitationStatusId)
                    .ToListAsync();
                var appliedStatusId = await _db.RoleInvitationStatuses
                    .Where(s => s.Code == InnovationToImpact.Domain.Roster.RoleInvitationStatusCodes.Applied)
                    .Select(s => s.Id)
                    .SingleAsync();
                foreach (var invitation in matchingInvitations)
                {
                    invitation.RoleInvitationStatusId = appliedStatusId;
                    invitation.RespondedAt = DateTime.UtcNow;
                    invitation.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
        else
        {
            user.Email = adIdentity.Email;
            user.Department = adIdentity.Department;
            user.ManagerEmail = adIdentity.ManagerEmail;
            user.Title = adIdentity.Title;
        }

        await _db.SaveChangesAsync();

        var roleCodes = user.IsActive ? user.UserRoles.Select(ur => ur.Role.Code).ToList() : new List<string>();

        return new CachedIdentityClaims(user.Id, user.SamAccountName, user.Email, user.Department, roleCodes);
    }

    private static string ExtractSamAccountName(string identityName)
    {
        var backslashIndex = identityName.IndexOf('\\');
        return backslashIndex >= 0 ? identityName[(backslashIndex + 1)..] : identityName;
    }
}
