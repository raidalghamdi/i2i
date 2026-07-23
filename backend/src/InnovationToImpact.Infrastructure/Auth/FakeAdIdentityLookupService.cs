using InnovationToImpact.Domain.Auth;

namespace InnovationToImpact.Infrastructure.Auth;

public class FakeAdIdentityLookupService : IAdIdentityLookupService
{
    private readonly Dictionary<string, AdIdentity> _identities;
    private readonly HashSet<string> _unavailableFor;
    private readonly Dictionary<string, IReadOnlyList<string>> _groupMemberships;

    public FakeAdIdentityLookupService(
        IEnumerable<AdIdentity>? seedIdentities = null,
        IEnumerable<string>? unavailableFor = null,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? groupMemberships = null)
    {
        _identities = (seedIdentities ?? DefaultIdentities())
            .ToDictionary(i => i.SamAccountName, StringComparer.OrdinalIgnoreCase);
        _unavailableFor = new HashSet<string>(unavailableFor ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        _groupMemberships = new Dictionary<string, IReadOnlyList<string>>(
            groupMemberships ?? new Dictionary<string, IReadOnlyList<string>>(),
            StringComparer.OrdinalIgnoreCase);
    }

    public int CallCount { get; private set; }

    public Task<AdIdentity?> ResolveAsync(string samAccountName, CancellationToken cancellationToken = default)
    {
        CallCount++;

        if (_unavailableFor.Contains(samAccountName))
        {
            throw new InvalidOperationException($"Simulated AD outage for '{samAccountName}'.");
        }

        _identities.TryGetValue(samAccountName, out var identity);
        return Task.FromResult(identity);
    }

    public Task<IReadOnlyList<AdIdentity>> ResolveGroupMembersAsync(string groupName, CancellationToken cancellationToken = default)
    {
        if (!_groupMemberships.TryGetValue(groupName, out var memberSamNames))
        {
            return Task.FromResult<IReadOnlyList<AdIdentity>>(Array.Empty<AdIdentity>());
        }

        var members = memberSamNames
            .Select(samName => _identities.TryGetValue(samName, out var identity) ? identity : null)
            .Where(identity => identity is not null)
            .Select(identity => identity!)
            .ToList();

        return Task.FromResult<IReadOnlyList<AdIdentity>>(members);
    }

    private static IEnumerable<AdIdentity> DefaultIdentities() => new[]
    {
        new AdIdentity("devuser", "Dev User", "devuser@gac-demo.sa", "Innovation", "Software Engineer", "manager@gac-demo.sa")
    };
}
