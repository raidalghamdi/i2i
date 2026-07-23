using System.Net;
using System.Net.Http.Json;
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
using Xunit;

namespace InnovationToImpact.Api.Tests;

public class ComplianceEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;

    public ComplianceEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("admin1", "Admin One", "admin1@gac-demo.sa", null, null, null),
            new AdIdentity("submitter1", "Submitter One", "submitter1@gac-demo.sa", null, null, null),
        });

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
                SeedUsersAndRoles(db);
            });
        });
    }

    private static void SeedUsersAndRoles(InnovationDbContext db)
    {
        var roleIds = db.Roles.ToDictionary(r => r.Code, r => r.Id);

        var adminId = Guid.NewGuid();
        db.Users.Add(new User { Id = adminId, SamAccountName = "admin1", Email = "admin1@gac-demo.sa", FullNameAr = "admin1", FullNameEn = "admin1" });
        var submitterId = Guid.NewGuid();
        db.Users.Add(new User { Id = submitterId, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "submitter1", FullNameEn = "submitter1" });
        db.SaveChanges();

        db.Set<UserRole>().Add(new UserRole { UserId = adminId, RoleId = roleIds[RoleCodes.Admin], IsPrimary = true });
        db.Set<UserRole>().Add(new UserRole { UserId = submitterId, RoleId = roleIds[RoleCodes.Submitter], IsPrimary = true });
        db.SaveChanges();
    }

    [Fact]
    public void SeedAppliesExpectedComplianceControlCount()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();

        Assert.Equal(11, db.ComplianceControls.Count());
        Assert.True(db.ComplianceControls.Any(c => c.ControlCode == "NDMO-DG-01"));
        Assert.True(db.ComplianceControls.Any(c => c.ControlCode == "WCAG-2.1-AA"));
    }

    [Fact]
    public async Task AdminGetsAllSeededControlsWithJoinsAndParsedPaths()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await client.GetAsync("/api/admin/compliance");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(11, items.GetArrayLength());

        var first = items.EnumerateArray().First();
        Assert.True(first.TryGetProperty("id", out _));
        Assert.True(first.TryGetProperty("controlCode", out _));
        Assert.True(first.TryGetProperty("standardBodyCode", out _));
        Assert.True(first.TryGetProperty("standardBodyNameAr", out _));
        Assert.True(first.TryGetProperty("standardBodyNameEn", out _));
        Assert.True(first.TryGetProperty("titleAr", out _));
        Assert.True(first.TryGetProperty("titleEn", out _));
        Assert.True(first.TryGetProperty("descriptionAr", out _));
        Assert.True(first.TryGetProperty("descriptionEn", out _));
        Assert.True(first.TryGetProperty("statusCode", out _));
        Assert.True(first.TryGetProperty("statusNameAr", out _));
        Assert.True(first.TryGetProperty("statusNameEn", out _));

        // First row must be the first sdaia_ndmo control by SortOrder then ControlCode.
        Assert.Equal("sdaia_ndmo", first.GetProperty("standardBodyCode").GetString());
        Assert.Equal("NDMO-DG-01", first.GetProperty("controlCode").GetString());
        Assert.Equal("met", first.GetProperty("statusCode").GetString());
        Assert.Equal("Met", first.GetProperty("statusNameEn").GetString());

        var mappedPaths = first.GetProperty("mappedFeaturePaths");
        Assert.Equal(JsonValueKind.Array, mappedPaths.ValueKind);
        var pathsList = mappedPaths.EnumerateArray().Select(p => p.GetString()).ToList();
        Assert.Contains("/admin/audit", pathsList);
        Assert.Contains("/admin/reports", pathsList);
    }

    [Fact]
    public async Task ItemsAreOrderedByStandardBodySortOrderThenControlCode()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await client.GetAsync("/api/admin/compliance");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<JsonElement>();
        var codes = items.EnumerateArray()
            .Select(i => (Body: i.GetProperty("standardBodyCode").GetString(), Code: i.GetProperty("controlCode").GetString()))
            .ToList();

        // sdaia_ndmo (SortOrder 1) controls must come before nca (SortOrder 2), etc.
        var bodyOrder = codes.Select(c => c.Body).Distinct().ToList();
        Assert.Equal(new[] { "sdaia_ndmo", "nca", "dga", "cst", "rdia" }, bodyOrder);

        // Within sdaia_ndmo, controls are ordered by ControlCode.
        var ndmoCodes = codes.Where(c => c.Body == "sdaia_ndmo").Select(c => c.Code).ToList();
        var sortedNdmoCodes = ndmoCodes.OrderBy(c => c, StringComparer.Ordinal).ToList();
        Assert.Equal(sortedNdmoCodes, ndmoCodes);
    }

    [Fact]
    public async Task NonAdminGetsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.GetAsync("/api/admin/compliance");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    public void Dispose() => _connection.Dispose();
}
