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

public class RolesCatalogEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;

    public RolesCatalogEndpointTests(WebApplicationFactory<Program> factory)
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

        // Deactivate one role so tests can assert the admin catalog still returns it (unlike the public /api/roles endpoint).
        var judgeRole = db.Roles.Single(r => r.Code == RoleCodes.Judge);
        judgeRole.IsActive = false;
        db.SaveChanges();
    }

    private InnovationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<InnovationDbContext>().UseSqlite(_connection).Options;
        return new InnovationDbContext(options);
    }

    [Fact]
    public async Task AdminGetsAllRolesInclInactive_OrderedBySortOrder()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await client.GetAsync("/api/admin/roles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var items = body.EnumerateArray().ToList();

        Assert.Equal(8, items.Count);

        var sortOrders = items.Select(i => i.GetProperty("sortOrder").GetInt32()).ToList();
        Assert.Equal(sortOrders.OrderBy(x => x), sortOrders);

        var judge = items.Single(i => i.GetProperty("code").GetString() == RoleCodes.Judge);
        Assert.False(judge.GetProperty("isActive").GetBoolean());
        Assert.True(judge.GetProperty("isSystem").GetBoolean());
        Assert.NotEqual(Guid.Empty, judge.GetProperty("id").GetGuid());

        var admin = items.Single(i => i.GetProperty("code").GetString() == RoleCodes.Admin);
        Assert.Equal("مسؤول", admin.GetProperty("nameAr").GetString());
        Assert.Equal("Admin", admin.GetProperty("nameEn").GetString());
    }

    [Fact]
    public async Task SubmitterGetForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.GetAsync("/api/admin/roles");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PatchRenamesTogglesActiveAndReorders_AndAudits()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        Guid mentorId;
        using (var db = CreateDbContext())
        {
            mentorId = db.Roles.Single(r => r.Code == RoleCodes.Mentor).Id;
        }

        var response = await client.PatchAsJsonAsync($"/api/admin/roles/{mentorId}", new
        {
            nameAr = "موجه معدّل",
            nameEn = "Mentor Updated",
            isActive = false,
            sortOrder = 99,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("موجه معدّل", body.GetProperty("nameAr").GetString());
        Assert.Equal("Mentor Updated", body.GetProperty("nameEn").GetString());
        Assert.False(body.GetProperty("isActive").GetBoolean());
        Assert.Equal(99, body.GetProperty("sortOrder").GetInt32());
        Assert.Equal(RoleCodes.Mentor, body.GetProperty("code").GetString());
        Assert.True(body.GetProperty("isSystem").GetBoolean());

        using var verifyDb = CreateDbContext();
        var role = verifyDb.Roles.Single(r => r.Id == mentorId);
        Assert.Equal("موجه معدّل", role.NameAr);
        Assert.Equal("Mentor Updated", role.NameEn);
        Assert.False(role.IsActive);
        Assert.Equal(99, role.SortOrder);
        // Code and IsSystem must never change via PATCH.
        Assert.Equal(RoleCodes.Mentor, role.Code);
        Assert.True(role.IsSystem);

        var auditRow = verifyDb.AuditLogs.SingleOrDefault(a => a.Action == "role.updated" && a.EntityId == mentorId);
        Assert.NotNull(auditRow);
    }

    [Fact]
    public async Task Patch_UnknownId_NotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await client.PatchAsJsonAsync($"/api/admin/roles/{Guid.NewGuid()}", new { nameEn = "Whatever" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SubmitterPatchForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        Guid mentorId;
        using (var db = CreateDbContext())
        {
            mentorId = db.Roles.Single(r => r.Code == RoleCodes.Mentor).Id;
        }

        var response = await client.PatchAsJsonAsync($"/api/admin/roles/{mentorId}", new { nameEn = "Whatever" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    public void Dispose() => _connection.Dispose();
}
