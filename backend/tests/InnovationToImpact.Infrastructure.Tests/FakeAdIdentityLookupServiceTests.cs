using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Infrastructure.Auth;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class FakeAdIdentityLookupServiceTests
{
    [Fact]
    public async Task ResolvesSeededIdentityCaseInsensitively()
    {
        var service = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("jsmith", "John Smith", "jsmith@gac-demo.sa", "Innovation", "Analyst", "mgr@gac-demo.sa")
        });

        var result = await service.ResolveAsync("JSMITH");

        Assert.NotNull(result);
        Assert.Equal("jsmith@gac-demo.sa", result!.Email);
    }

    [Fact]
    public async Task ReturnsNullForUnknownSamAccountName()
    {
        var service = new FakeAdIdentityLookupService(Array.Empty<AdIdentity>());

        var result = await service.ResolveAsync("nobody");

        Assert.Null(result);
    }

    [Fact]
    public async Task ThrowsForNamesMarkedUnavailable()
    {
        var service = new FakeAdIdentityLookupService(Array.Empty<AdIdentity>(), unavailableFor: new[] { "downuser" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResolveAsync("downuser"));
    }

    [Fact]
    public async Task TracksCallCount()
    {
        var service = new FakeAdIdentityLookupService(Array.Empty<AdIdentity>());

        await service.ResolveAsync("a");
        await service.ResolveAsync("b");

        Assert.Equal(2, service.CallCount);
    }

    [Fact]
    public async Task ResolveGroupMembersAsync_ReturnsResolvedMembersForSeededGroup()
    {
        var service = new FakeAdIdentityLookupService(
            seedIdentities: new[]
            {
                new AdIdentity("jsmith", "John Smith", "jsmith@gac-demo.sa", "Innovation", "Analyst", null),
                new AdIdentity("abdullah.k", "Abdullah K", "abdullah.k@gac-demo.sa", "Operations", "Analyst", null),
            },
            groupMemberships: new Dictionary<string, IReadOnlyList<string>>
            {
                ["GAC-Evaluators"] = new[] { "jsmith", "abdullah.k" },
            });

        var members = await service.ResolveGroupMembersAsync("GAC-Evaluators");

        Assert.Equal(2, members.Count);
        Assert.Contains(members, m => m.SamAccountName == "jsmith");
        Assert.Contains(members, m => m.SamAccountName == "abdullah.k");
    }

    [Fact]
    public async Task ResolveGroupMembersAsync_UnknownGroup_ReturnsEmpty()
    {
        var service = new FakeAdIdentityLookupService(Array.Empty<AdIdentity>());

        var members = await service.ResolveGroupMembersAsync("nonexistent-group");

        Assert.Empty(members);
    }

    [Fact]
    public async Task ResolveGroupMembersAsync_IsCaseInsensitiveOnGroupName()
    {
        var service = new FakeAdIdentityLookupService(
            seedIdentities: new[] { new AdIdentity("jsmith", "John Smith", "jsmith@gac-demo.sa", "Innovation", "Analyst", null) },
            groupMemberships: new Dictionary<string, IReadOnlyList<string>>
            {
                ["GAC-Evaluators"] = new[] { "jsmith" },
            });

        var members = await service.ResolveGroupMembersAsync("gac-evaluators");

        Assert.Single(members);
    }
}
