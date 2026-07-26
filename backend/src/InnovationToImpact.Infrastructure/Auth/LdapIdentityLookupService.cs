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

    public Task<IReadOnlyList<AdIdentity>> SearchByNameAsync(string query, int maxResults, CancellationToken ct = default)
    {
        return Task.Run(() => SearchByName(query, maxResults), ct);
    }

    public Task<IReadOnlyList<AdIdentity>> ListAllAsync(int maxResults, CancellationToken ct = default)
    {
        return Task.Run(() => ListAll(maxResults), ct);
    }

    private IReadOnlyList<AdIdentity> ListAll(int maxResults)
    {
        const string filter = "(&(objectCategory=person)(objectClass=user))";

        using var root = new DirectoryEntry(
            $"LDAP://{_options.Domain}",
            _options.ServiceAccountUsername,
            _options.ServiceAccountPassword);
        using var searcher = new DirectorySearcher(root)
        {
            Filter = filter,
            SizeLimit = maxResults,
            PageSize = Math.Min(maxResults, 1000),
        };
        searcher.PropertiesToLoad.AddRange(new[] { "samAccountName", "displayName", "mail", "department", "title" });

        var results = new List<AdIdentity>();
        using var searchResults = searcher.FindAll();
        foreach (SearchResult result in searchResults)
        {
            var samAccountName = GetSearchResultProperty(result, "samAccountName") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(samAccountName))
            {
                continue;
            }
            results.Add(new AdIdentity(
                SamAccountName: samAccountName,
                DisplayName: GetSearchResultProperty(result, "displayName") ?? samAccountName,
                Email: GetSearchResultProperty(result, "mail") ?? string.Empty,
                Department: GetSearchResultProperty(result, "department"),
                Title: GetSearchResultProperty(result, "title"),
                ManagerEmail: null));
            if (results.Count >= maxResults)
            {
                break;
            }
        }

        return results;
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

    private IReadOnlyList<AdIdentity> SearchByName(string query, int maxResults)
    {
        var trimmed = query?.Trim() ?? string.Empty;
        if (trimmed.Length < 2)
        {
            return Array.Empty<AdIdentity>();
        }

        var escaped = EscapeLdapFilterValue(trimmed);
        var filter = $"(&(objectCategory=person)(objectClass=user)(|(displayName=*{escaped}*)(samAccountName=*{escaped}*)))";

        using var root = new DirectoryEntry(
            $"LDAP://{_options.Domain}",
            _options.ServiceAccountUsername,
            _options.ServiceAccountPassword);
        using var searcher = new DirectorySearcher(root)
        {
            Filter = filter,
            SizeLimit = maxResults,
            PageSize = maxResults,
        };
        searcher.PropertiesToLoad.AddRange(new[] { "samAccountName", "displayName", "mail", "department", "title" });

        var results = new List<AdIdentity>();
        using var searchResults = searcher.FindAll();
        foreach (SearchResult result in searchResults)
        {
            var samAccountName = GetSearchResultProperty(result, "samAccountName") ?? string.Empty;
            results.Add(new AdIdentity(
                SamAccountName: samAccountName,
                DisplayName: GetSearchResultProperty(result, "displayName") ?? samAccountName,
                Email: GetSearchResultProperty(result, "mail") ?? string.Empty,
                Department: GetSearchResultProperty(result, "department"),
                Title: GetSearchResultProperty(result, "title"),
                ManagerEmail: null));
        }

        return results;
    }

    private static string? GetSearchResultProperty(SearchResult result, string propertyName)
    {
        if (!result.Properties.Contains(propertyName) || result.Properties[propertyName].Count == 0)
        {
            return null;
        }

        return result.Properties[propertyName][0] as string;
    }

    private static string EscapeLdapFilterValue(string value)
    {
        return value
            .Replace("\\", "\\5c")
            .Replace("*", "\\2a")
            .Replace("(", "\\28")
            .Replace(")", "\\29")
            .Replace("\0", "\\00");
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
