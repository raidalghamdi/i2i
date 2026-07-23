using System.Net;
using System.Text.Json;
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

public class TermsSeedTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;

    public TermsSeedTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        // Null seeds the fake's built-in "devuser" default identity, matching appsettings.Development.json's
        // DevAuth:SamAccountName fallback used when no X-Dev-User header is sent.
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

    private InnovationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<InnovationDbContext>().UseSqlite(_connection).Options;
        return new InnovationDbContext(options);
    }

    [Fact]
    public void TermsCmsContentRow_IsSeeded()
    {
        _factory.CreateClient(); // forces host build so ConfigureServices (EnsureCreated) has run

        using var db = CreateDbContext();
        var terms = db.CmsContents.Single(c => c.Slug == "terms");

        Assert.Equal("الشروط والأحكام", terms.TitleAr);
        Assert.Equal("Terms & Conditions", terms.TitleEn);
        Assert.True(terms.IsPublished);
        Assert.False(string.IsNullOrWhiteSpace(terms.BodyAr));
        Assert.False(string.IsNullOrWhiteSpace(terms.BodyEn));
        Assert.NotNull(terms.PublishedAt);
    }

    [Fact]
    public async Task PublicTermsEndpoint_ReturnsSeededContent()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/public/cms/content/terms");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("terms", body.GetProperty("slug").GetString());
        Assert.Equal("Terms & Conditions", body.GetProperty("titleEn").GetString());
        Assert.Equal("الشروط والأحكام", body.GetProperty("titleAr").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("bodyAr").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("bodyEn").GetString()));
    }

    public void Dispose() => _connection.Dispose();
}
