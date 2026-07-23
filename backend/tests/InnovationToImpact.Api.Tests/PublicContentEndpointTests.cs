using System.Net;
using System.Text.Json;
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

public class PublicContentEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;

    public PublicContentEndpointTests(WebApplicationFactory<Program> factory)
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
                db.CmsContents.Add(new CmsContent { Id = Guid.NewGuid(), Slug = "about", TitleAr = "ع", TitleEn = "About", BodyAr = "ب", BodyEn = "Body", IsPublished = true });
                db.CmsContents.Add(new CmsContent { Id = Guid.NewGuid(), Slug = "secret", TitleAr = "س", TitleEn = "Secret", BodyAr = "ب", BodyEn = "B", IsPublished = false });
                db.SaveChanges();
            });
        });
    }

    [Fact]
    public async Task PublishedSlug_Anonymous_ReturnsContent()
    {
        var client = _factory.CreateClient(); // NO X-Dev-User header — anonymous
        var res = await client.GetAsync("/api/public/cms/content/about");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("About", body.GetProperty("titleEn").GetString());
        Assert.Equal("about", body.GetProperty("slug").GetString());
    }

    [Fact]
    public async Task UnpublishedSlug_Returns404()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/public/cms/content/secret");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task MissingSlug_Returns404()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/public/cms/content/nope");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    public void Dispose() => _connection.Dispose();
}
