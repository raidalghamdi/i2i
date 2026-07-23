using System.Net;
using System.Net.Http.Json;
using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Auth;
using InnovationToImpact.Infrastructure.Data;
using InnovationToImpact.Infrastructure.Ideas;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace InnovationToImpact.Api.Tests;

public class UserManagementEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"evidence-storage-test-{Guid.NewGuid():N}");

    public UserManagementEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var lookup = new FakeAdIdentityLookupService(
            seedIdentities: new[]
            {
                new AdIdentity("admin1", "Admin One", "admin1@gac-demo.sa", null, null, null),
                new AdIdentity("submitter1", "Submitter One", "submitter1@gac-demo.sa", null, null, null),
                new AdIdentity("newperson1", "New Person One", "newperson1@gac-demo.sa", null, null, null),
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

                services.Configure<EvidenceStorageOptions>(options => options.RootPath = _rootPath);

                using var scope = services.BuildServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
                db.Database.EnsureCreated();
                SeedAdmin(db);
            });
        });
    }

    private static void SeedAdmin(InnovationDbContext db)
    {
        var adminRoleId = db.Roles.Single(r => r.Code == "admin").Id;
        var adminId = Guid.NewGuid();
        db.Users.Add(new User { Id = adminId, SamAccountName = "admin1", Email = "admin1@gac-demo.sa", FullNameAr = "admin1", FullNameEn = "admin1" });
        db.SaveChanges();
        db.Set<UserRole>().Add(new UserRole { UserId = adminId, RoleId = adminRoleId, IsPrimary = true });
        db.SaveChanges();
    }

    [Fact]
    public async Task GrantRole_ToNotYetKnownUser_IsPendingAndAppliesOnFirstLogin()
    {
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var grantResponse = await adminClient.PostAsJsonAsync("/api/admin/role-grants", new { samAccountName = "newperson1", roleCode = "evaluator" });
        Assert.Equal(HttpStatusCode.OK, grantResponse.StatusCode);
        var grantBody = await grantResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"pending\"", grantBody);

        var pendingResponse = await adminClient.GetAsync("/api/admin/pending-role-grants");
        var pendingBody = await pendingResponse.Content.ReadAsStringAsync();
        Assert.Contains("newperson1", pendingBody);

        var newPersonClient = _factory.CreateClient();
        newPersonClient.DefaultRequestHeaders.Add("X-Dev-User", "newperson1");
        var meResponse = await newPersonClient.GetAsync("/api/identity/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        var meBody = await meResponse.Content.ReadAsStringAsync();
        Assert.Contains("evaluator", meBody);
    }

    [Fact]
    public async Task RoleGrants_WithSubmitterRole_ReturnsForbidden()
    {
        var submitterClient = _factory.CreateClient();
        submitterClient.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await submitterClient.PostAsJsonAsync("/api/admin/role-grants", new { samAccountName = "newperson1", roleCode = "evaluator" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    public void Dispose()
    {
        _connection.Dispose();
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
