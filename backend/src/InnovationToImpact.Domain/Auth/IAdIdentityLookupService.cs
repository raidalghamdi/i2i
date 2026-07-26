namespace InnovationToImpact.Domain.Auth;

public interface IAdIdentityLookupService
{
    Task<AdIdentity?> ResolveAsync(string samAccountName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdIdentity>> ResolveGroupMembersAsync(string groupName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdIdentity>> SearchByNameAsync(string query, int maxResults, CancellationToken ct = default);

    /// <summary>Enumerates every person in the directory (bounded by <paramref name="maxResults"/>),
    /// for a bulk "import all users from AD" operation.</summary>
    Task<IReadOnlyList<AdIdentity>> ListAllAsync(int maxResults, CancellationToken ct = default);
}
