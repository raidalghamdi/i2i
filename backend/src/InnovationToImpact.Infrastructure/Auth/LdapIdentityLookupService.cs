using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using InnovationToImpact.Domain.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InnovationToImpact.Infrastructure.Auth;

public class LdapIdentityLookupService : IAdIdentityLookupService
{
    private readonly ActiveDirectoryOptions _options;
    private readonly ILogger<LdapIdentityLookupService> _logger;

    public LdapIdentityLookupService(IOptions<ActiveDirectoryOptions> options, ILogger<LdapIdentityLookupService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<AdIdentity?> ResolveAsync(string samAccountName, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Resolve(samAccountName), cancellationToken);
    }

    public Task<IReadOnlyList<AdIdentity>> ResolveGroupMembersAsync(string groupName, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => ResolveGroupMembers(groupName), cancellationToken);
    }

    private AdIdentity? Resolve(string samAccountName)
    {
        using var context = new PrincipalContext(
            ContextType.Domain,
            _options.Domain,
            _options.ServiceAccountUsername,
            _options.ServiceAccountPassword);

        using var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, samAccountName);
        if (user is null)
        {
            _logger.LogWarning("AD lookup found no user for SamAccountName {SamAccountName}", samAccountName);
            return null;
        }

        return MapToAdIdentity(user, samAccountName);
    }

    private IReadOnlyList<AdIdentity> ResolveGroupMembers(string groupName)
    {
        using var context = new PrincipalContext(
            ContextType.Domain,
            _options.Domain,
            _options.ServiceAccountUsername,
            _options.ServiceAccountPassword);

        using var group = GroupPrincipal.FindByIdentity(context, IdentityType.SamAccountName, groupName);
        if (group is null)
        {
            _logger.LogWarning("AD lookup found no group for name {GroupName}", groupName);
            return Array.Empty<AdIdentity>();
        }

        var members = new List<AdIdentity>();
        foreach (var principal in group.GetMembers())
        {
            if (principal is UserPrincipal userPrincipal)
            {
                members.Add(MapToAdIdentity(userPrincipal));
            }
        }
        return members;
    }

    private AdIdentity MapToAdIdentity(UserPrincipal user, string? fallbackSamAccountName = null)
    {
        string? managerEmail = null;
        var managerDn = GetSingleAttribute(user, "manager");
        if (!string.IsNullOrEmpty(managerDn))
        {
            using var managerContext = new PrincipalContext(
                ContextType.Domain,
                _options.Domain,
                _options.ServiceAccountUsername,
                _options.ServiceAccountPassword);
            using var manager = UserPrincipal.FindByIdentity(managerContext, IdentityType.DistinguishedName, managerDn);
            managerEmail = manager?.EmailAddress;
        }

        return new AdIdentity(
            SamAccountName: user.SamAccountName ?? fallbackSamAccountName ?? string.Empty,
            DisplayName: user.DisplayName ?? user.SamAccountName ?? fallbackSamAccountName ?? string.Empty,
            Email: user.EmailAddress ?? string.Empty,
            Department: GetSingleAttribute(user, "department"),
            Title: GetSingleAttribute(user, "title"),
            ManagerEmail: managerEmail);
    }

    private static string? GetSingleAttribute(UserPrincipal user, string attributeName)
    {
        var directoryEntry = user.GetUnderlyingObject() as DirectoryEntry;
        return directoryEntry?.Properties[attributeName]?.Value as string;
    }
}
