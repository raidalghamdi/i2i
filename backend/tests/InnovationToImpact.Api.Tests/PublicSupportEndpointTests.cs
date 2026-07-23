using System.Net;
using System.Net.Http.Json;
using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Auth;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InnovationToImpact.Api.Tests;

public class PublicSupportEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;

    public PublicSupportEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        // Null seeds the fake's built-in "devuser" default identity, matching appsettings.Development.json's
        // DevAuth:SamAccountName fallback used when no X-Dev-User header is sent (Array.Empty would remove it
        // and cause identity resolution to fail with a 503 before the anonymous endpoint is ever reached).
        var lookup = new FakeAdIdentityLookupService(null);
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<InnovationDbContext>>();
                services.AddDbContext<InnovationDbContext>(options => options.UseSqlite(_connection));
                services.RemoveAll<IAdIdentityLookupService>();
                services.AddSingleton<IAdIdentityLookupService>(lookup);
                using var scope = services.BuildServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
                db.Database.EnsureCreated();
            });
        });
    }

    [Fact]
    public async Task ValidSubmission_Anonymous_ReturnsOkAndPersists()
    {
        var client = _factory.CreateClient(); // NO X-Dev-User header — anonymous
        var res = await client.PostAsJsonAsync("/api/public/support", new
        {
            name = "Jane Doe",
            email = "jane@example.com",
            subject = "Question",
            message = "I need help with something."
        });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
        Assert.Equal(1, await db.SupportMessages.CountAsync());
        var saved = await db.SupportMessages.SingleAsync();
        Assert.Equal("jane@example.com", saved.Email);
        Assert.Equal("I need help with something.", saved.Body);
    }

    [Fact]
    public async Task BlankMessage_Returns400()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/public/support", new
        {
            name = "Jane Doe",
            email = "jane@example.com",
            subject = "Question",
            message = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task BlankEmail_Returns400()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/public/support", new
        {
            name = "Jane Doe",
            email = "",
            subject = "Question",
            message = "I need help with something."
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task NullNameAndSubject_PersistsAsEmpty()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/public/support", new
        {
            email = "jane@example.com",
            message = "I need help with something."
        });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
        var saved = await db.SupportMessages.SingleAsync();
        Assert.Equal(string.Empty, saved.Name);
        Assert.Equal(string.Empty, saved.Subject);
    }

    [Fact]
    public async Task OverLengthName_Returns400()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/public/support", new
        {
            name = new string('a', 201),
            email = "jane@example.com",
            subject = "Question",
            message = "I need help with something."
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    public void Dispose() => _connection.Dispose();
}
