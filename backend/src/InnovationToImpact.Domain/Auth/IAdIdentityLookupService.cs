namespace InnovationToImpact.Domain.Auth;

public interface IAdIdentityLookupService
{
    Task<AdIdentity?> ResolveAsync(string samAccountName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdIdentity>> ResolveGroupMembersAsync(string groupName, CancellationToken cancellationToken = default);
}
