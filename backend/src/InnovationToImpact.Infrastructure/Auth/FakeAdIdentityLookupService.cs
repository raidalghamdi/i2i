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

    public Task<IReadOnlyList<AdIdentity>> SearchByNameAsync(string query, int maxResults, CancellationToken ct = default)
    {
        if (_unavailableFor.Contains(query))
        {
            throw new InvalidOperationException($"Simulated AD outage for '{query}'.");
        }

        var trimmed = query?.Trim() ?? string.Empty;
        if (trimmed.Length < 2)
        {
            return Task.FromResult<IReadOnlyList<AdIdentity>>(Array.Empty<AdIdentity>());
        }

        var matches = _identities.Values
            .Where(i => i.DisplayName.Contains(trimmed, StringComparison.OrdinalIgnoreCase)
                || i.SamAccountName.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
            .Take(maxResults)
            .ToList();

        return Task.FromResult<IReadOnlyList<AdIdentity>>(matches);
    }

    public Task<IReadOnlyList<AdIdentity>> ListAllAsync(int maxResults, CancellationToken ct = default)
    {
        var all = _identities.Values
            .OrderBy(i => i.SamAccountName, StringComparer.OrdinalIgnoreCase)
            .Take(maxResults)
            .ToList();
        return Task.FromResult<IReadOnlyList<AdIdentity>>(all);
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

    private static IEnumerable<AdIdentity> DefaultIdentities()
    {
        var baseIdentities = new[]
        {
            new AdIdentity("devuser", "Dev User", "devuser@gac-demo.sa", "Innovation", "Software Engineer", "manager@gac-demo.sa"),
            new AdIdentity("lhassan", "Layla Hassan", "lhassan@gac-demo.sa", "Innovation", "Product Manager", "devuser@gac-demo.sa"),
            new AdIdentity("ofarouk", "Omar Farouk", "ofarouk@gac-demo.sa", "Operations", "Operations Lead", "devuser@gac-demo.sa"),
            new AdIdentity("salqahtani", "Sara Al-Qahtani", "salqahtani@gac-demo.sa", "Finance", "Financial Analyst", "lhassan@gac-demo.sa"),
            new AdIdentity("knasser", "Khalid Nasser", "knasser@gac-demo.sa", "IT", "Systems Administrator", "ofarouk@gac-demo.sa"),
            new AdIdentity("nsaleh", "Noura Saleh", "nsaleh@gac-demo.sa", "Innovation", "Evaluator", "lhassan@gac-demo.sa"),
            new AdIdentity("fzahra", "Fatima Zahra", "fzahra@gac-demo.sa", "Human Resources", "HR Specialist", "salqahtani@gac-demo.sa"),
            new AdIdentity("yibrahim", "Yousef Ibrahim", "yibrahim@gac-demo.sa", "Innovation", "Committee Member", "lhassan@gac-demo.sa"),
            new AdIdentity("madel", "Mona Adel", "madel@gac-demo.sa", "Marketing", "Communications Officer", "ofarouk@gac-demo.sa"),
            new AdIdentity("taziz", "Tariq Aziz", "taziz@gac-demo.sa", "Innovation", "Supervisor", "devuser@gac-demo.sa"),
        };

        // DEV/TEST: 5 identities per role (40) so every role can be exercised. See DevTestDirectory.
        return baseIdentities.Concat(DevTestDirectory.AsAdIdentities());
    }
}
