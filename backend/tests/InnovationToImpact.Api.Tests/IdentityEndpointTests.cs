using System.Net;
using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Infrastructure.Auth;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace InnovationToImpact.Api.Tests;

public class IdentityEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly FakeAdIdentityLookupService _lookup;
    private readonly WebApplicationFactory<Program> _factory;

    public IdentityEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("devuser", "Dev User", "devuser@gac-demo.sa", "Innovation", "Engineer", null)
        });

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<InnovationDbContext>>();
                services.AddDbContext<InnovationDbContext>(options => options.UseSqlite(_connection));

                services.RemoveAll<IAdIdentityLookupService>();
                services.AddSingleton<IAdIdentityLookupService>(_lookup);

                using var scope = services.BuildServiceProvider().CreateScope();
                scope.ServiceProvider.GetRequiredService<InnovationDbContext>().Database.EnsureCreated();
            });
        });
    }

    [Fact]
    public async Task AuthenticatedRequest_ReturnsIdentityWithNoRoles()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "devuser");

        var response = await client.GetAsync("/api/identity/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"samAccountName\":\"devuser\"", body);
        Assert.Contains("\"email\":\"devuser@gac-demo.sa\"", body);
        Assert.Contains("\"roles\":[]", body);
    }

    [Fact]
    public async Task SecondRequestForSameUser_DoesNotCallLookupServiceAgain()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "devuser");

        await client.GetAsync("/api/identity/me");
        await client.GetAsync("/api/identity/me");

        Assert.Equal(1, _lookup.CallCount);
    }

    [Fact]
    public async Task UnknownUser_Returns503()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "nobodyknown");

        var response = await client.GetAsync("/api/identity/me");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    public void Dispose() => _connection.Dispose();
}
