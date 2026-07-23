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
using Xunit;

namespace InnovationToImpact.Api.Tests;

public class CommitteeCriteriaEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;

    public CommitteeCriteriaEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("admin1", "Admin One", "admin1@gac-demo.sa", null, null, null),
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
        var supervisorId = Guid.NewGuid();
        db.Users.Add(new User { Id = supervisorId, SamAccountName = "supervisor1", Email = "supervisor1@gac-demo.sa", FullNameAr = "supervisor1", FullNameEn = "supervisor1" });
        var submitterId = Guid.NewGuid();
        db.Users.Add(new User { Id = submitterId, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "submitter1", FullNameEn = "submitter1" });
        db.SaveChanges();

        db.Set<UserRole>().Add(new UserRole { UserId = adminId, RoleId = roleIds[RoleCodes.Admin], IsPrimary = true });
        db.Set<UserRole>().Add(new UserRole { UserId = supervisorId, RoleId = roleIds[RoleCodes.Supervisor], IsPrimary = true });
        db.Set<UserRole>().Add(new UserRole { UserId = submitterId, RoleId = roleIds[RoleCodes.Submitter], IsPrimary = true });
        db.SaveChanges();
    }

    [Fact]
    public async Task AdminGetAll_ReturnsAllSeededCriteriaWithFullFields()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await client.GetAsync("/api/admin/committee-criteria");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"code\":\"originality\"", body);
        Assert.Contains("\"code\":\"feasibility\"", body);
        Assert.Contains("\"code\":\"impact\"", body);
        Assert.Contains("\"code\":\"alignment\"", body);
        Assert.Contains("\"active\":true", body);
        Assert.Contains("\"nameAr\"", body);
        Assert.Contains("\"nameEn\"", body);
        Assert.Contains("\"weight\"", body);
    }

    [Fact]
    public async Task SupervisorGetAll_ReturnsOk()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.GetAsync("/api/admin/committee-criteria");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SubmitterGetAll_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.GetAsync("/api/admin/committee-criteria");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_CreatesCriterionAndWritesAuditRow()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var input = new
        {
            code = "clarity",
            nameAr = "الوضوح",
            nameEn = "Clarity",
            descriptionAr = (string?)null,
            descriptionEn = (string?)null,
            weight = 0.10m,
            active = true,
        };

        var response = await client.PostAsJsonAsync("/api/admin/committee-criteria", input);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"code\":\"clarity\"", body);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
        var created = await db.CommitteeCriteria.SingleAsync(c => c.Code == "clarity");
        var auditRow = await db.AuditLogs.SingleOrDefaultAsync(a => a.EntityType == "committee_criterion" && a.EntityId == created.Id && a.Action == "committee_criterion.created");
        Assert.NotNull(auditRow);
    }

    [Fact]
    public async Task Post_DuplicateCode_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var input = new
        {
            code = "originality",
            nameAr = "الأصالة 2",
            nameEn = "Originality 2",
            descriptionAr = (string?)null,
            descriptionEn = (string?)null,
            weight = 0.10m,
            active = true,
        };

        var response = await client.PostAsJsonAsync("/api/admin/committee-criteria", input);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdatesExistingCriterion()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        Guid criterionId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
            criterionId = (await db.CommitteeCriteria.SingleAsync(c => c.Code == "feasibility")).Id;
        }

        var input = new
        {
            code = "feasibility",
            nameAr = "قابلية التنفيذ المحدثة",
            nameEn = "Feasibility Updated",
            descriptionAr = (string?)null,
            descriptionEn = (string?)null,
            weight = 0.20m,
            active = false,
        };

        var response = await client.PutAsJsonAsync($"/api/admin/committee-criteria/{criterionId}", input);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"nameEn\":\"Feasibility Updated\"", body);
        Assert.Contains("\"active\":false", body);
    }

    [Fact]
    public async Task Put_NotFound_Returns404()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var input = new
        {
            code = "ghost",
            nameAr = "غير موجود",
            nameEn = "Ghost",
            descriptionAr = (string?)null,
            descriptionEn = (string?)null,
            weight = 0.10m,
            active = true,
        };

        var response = await client.PutAsJsonAsync($"/api/admin/committee-criteria/{Guid.NewGuid()}", input);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesCriterion()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        Guid criterionId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
            criterionId = (await db.CommitteeCriteria.SingleAsync(c => c.Code == "alignment")).Id;
        }

        var response = await client.DeleteAsync($"/api/admin/committee-criteria/{criterionId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<InnovationDbContext>();
        Assert.False(await db2.CommitteeCriteria.AnyAsync(c => c.Id == criterionId));
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await client.DeleteAsync($"/api/admin/committee-criteria/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public void Dispose() => _connection.Dispose();
}
