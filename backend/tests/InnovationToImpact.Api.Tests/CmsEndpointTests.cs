using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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

public class CmsEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"cms-endpoint-test-{Guid.NewGuid():N}");

    public CmsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var lookup = new FakeAdIdentityLookupService(
            seedIdentities: new[]
            {
                new AdIdentity("cmsadmin1", "Cms Admin One", "cmsadmin1@gac-demo.sa", null, null, null),
                new AdIdentity("supervisor1", "Supervisor One", "supervisor1@gac-demo.sa", null, null, null),
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

                services.Configure<EvidenceStorageOptions>(options => options.RootPath = _rootPath);

                using var scope = services.BuildServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
                db.Database.EnsureCreated();
                SeedAdmin(db);
                SeedSupervisor(db);
                SeedSubmitter(db);
            });
        });
    }

    private static void SeedAdmin(InnovationDbContext db)
    {
        var adminRoleId = db.Roles.Single(r => r.Code == "admin").Id;
        var adminId = Guid.NewGuid();
        db.Users.Add(new User { Id = adminId, SamAccountName = "cmsadmin1", Email = "cmsadmin1@gac-demo.sa", FullNameAr = "cmsadmin1", FullNameEn = "cmsadmin1" });
        db.SaveChanges();
        db.Set<UserRole>().Add(new UserRole { UserId = adminId, RoleId = adminRoleId, IsPrimary = true });
        db.SaveChanges();
    }

    private static void SeedSupervisor(InnovationDbContext db)
    {
        var supervisorRoleId = db.Roles.Single(r => r.Code == "supervisor").Id;
        var supervisorId = Guid.NewGuid();
        db.Users.Add(new User { Id = supervisorId, SamAccountName = "supervisor1", Email = "supervisor1@gac-demo.sa", FullNameAr = "supervisor1", FullNameEn = "supervisor1" });
        db.SaveChanges();
        db.Set<UserRole>().Add(new UserRole { UserId = supervisorId, RoleId = supervisorRoleId, IsPrimary = true });
        db.SaveChanges();
    }

    private static void SeedSubmitter(InnovationDbContext db)
    {
        var submitterRoleId = db.Roles.Single(r => r.Code == "submitter").Id;
        var submitterId = Guid.NewGuid();
        db.Users.Add(new User { Id = submitterId, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "submitter1", FullNameEn = "submitter1" });
        db.SaveChanges();
        db.Set<UserRole>().Add(new UserRole { UserId = submitterId, RoleId = submitterRoleId, IsPrimary = true });
        db.SaveChanges();
    }

    [Fact]
    public async Task CreateUpdateDeleteBlock_AsAdmin_PersistsAuditLogRowsForEachStep()
    {
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Add("X-Dev-User", "cmsadmin1");

        var createResponse = await adminClient.PostAsJsonAsync("/api/admin/cms/blocks", new { key = "hero-banner", contentAr = "أ", contentEn = "Hero", isPublished = true });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var blockId = created.GetProperty("id").GetGuid();

        var updateResponse = await adminClient.PutAsJsonAsync($"/api/admin/cms/blocks/{blockId}", new { key = "hero-banner", contentAr = "ب", contentEn = "Hero Updated", isPublished = false });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var deleteResponse = await adminClient.DeleteAsync($"/api/admin/cms/blocks/{blockId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
        var auditEntries = db.AuditLogs.Where(a => a.EntityType == "cms_block" && a.EntityId == blockId).OrderBy(a => a.ChainSeq).ToList();
        Assert.Equal(new[] { "create", "update", "delete" }, auditEntries.Select(a => a.Action));
    }

    [Fact]
    public async Task CreateBlock_AsNonAdmin_ReturnsForbidden()
    {
        var submitterClient = _factory.CreateClient();
        submitterClient.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await submitterClient.PostAsJsonAsync("/api/admin/cms/blocks", new { key = "unauthorized-block", contentAr = "أ", contentEn = "X", isPublished = true });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateBlock_DuplicateKey_ReturnsBadRequest()
    {
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Add("X-Dev-User", "cmsadmin1");
        await adminClient.PostAsJsonAsync("/api/admin/cms/blocks", new { key = "dup-key", contentAr = "أ", contentEn = "X", isPublished = true });

        var response = await adminClient.PostAsJsonAsync("/api/admin/cms/blocks", new { key = "dup-key", contentAr = "ب", contentEn = "Y", isPublished = true });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetBlocks_AsSupervisor_Succeeds()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.GetAsync("/api/admin/cms/blocks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetContent_AsSupervisor_Succeeds()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.GetAsync("/api/admin/cms/content");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetStrings_AsNonSupervisorNonAdmin_Forbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.GetAsync("/api/admin/cms/strings");

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
