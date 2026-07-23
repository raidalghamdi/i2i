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

public class ReportTitlesEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;

    public ReportTitlesEndpointTests(WebApplicationFactory<Program> factory)
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

    private InnovationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<InnovationDbContext>().UseSqlite(_connection).Options;
        return new InnovationDbContext(options);
    }

    [Fact]
    public async Task AdminGetsAllReportTitles_OrderedBySortOrderThenTitleEn()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        int expectedCount;
        using (var db = CreateDbContext())
        {
            expectedCount = db.ReportTitles.Count();
        }

        var response = await client.GetAsync("/api/admin/report-titles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var items = body.EnumerateArray().ToList();

        Assert.Equal(expectedCount, items.Count);

        var sortOrders = items.Select(i => i.GetProperty("sortOrder").GetInt32()).ToList();
        Assert.Equal(sortOrders.OrderBy(x => x), sortOrders);

        var ideasTitle = items.Single(i => i.GetProperty("key").GetString() == "ideas");
        Assert.Equal("سجل الأفكار", ideasTitle.GetProperty("titleAr").GetString());
        Assert.Equal("Ideas Register", ideasTitle.GetProperty("titleEn").GetString());
        Assert.NotEqual(Guid.Empty, ideasTitle.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task SubmitterGetForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.GetAsync("/api/admin/report-titles");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostCreatesReportTitle_AndAudits()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await client.PostAsJsonAsync("/api/admin/report-titles", new
        {
            key = "custom_export",
            titleAr = "تصدير مخصص",
            titleEn = "Custom Export",
            sortOrder = 100,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var newId = body.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, newId);

        using var verifyDb = CreateDbContext();
        var row = verifyDb.ReportTitles.Single(r => r.Id == newId);
        Assert.Equal("custom_export", row.Key);
        Assert.Equal("تصدير مخصص", row.TitleAr);
        Assert.Equal("Custom Export", row.TitleEn);
        Assert.Equal(100, row.SortOrder);

        var auditRow = verifyDb.AuditLogs.SingleOrDefault(a => a.Action == "report_title.created" && a.EntityId == newId);
        Assert.NotNull(auditRow);

        var getResponse = await client.GetAsync("/api/admin/report-titles");
        var getBody = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync()).RootElement;
        Assert.Contains(getBody.EnumerateArray(), i => i.GetProperty("key").GetString() == "custom_export");
    }

    [Fact]
    public async Task PostDuplicateKey_BadRequest()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await client.PostAsJsonAsync("/api/admin/report-titles", new
        {
            key = "ideas",
            titleAr = "تصدير مكرر",
            titleEn = "Duplicate",
            sortOrder = 200,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitterPostForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.PostAsJsonAsync("/api/admin/report-titles", new
        {
            key = "forbidden_key",
            titleAr = "غير مسموح",
            titleEn = "Forbidden",
            sortOrder = 1,
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PutUpdatesTitleArTitleEnSortOrder_ButNotKey_AndAudits()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        Guid ideasId;
        using (var db = CreateDbContext())
        {
            ideasId = db.ReportTitles.Single(r => r.Key == "ideas").Id;
        }

        var response = await client.PutAsJsonAsync($"/api/admin/report-titles/{ideasId}", new
        {
            titleAr = "سجل الأفكار المحدث",
            titleEn = "Ideas Register Updated",
            sortOrder = 42,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var verifyDb = CreateDbContext();
        var row = verifyDb.ReportTitles.Single(r => r.Id == ideasId);
        Assert.Equal("ideas", row.Key);
        Assert.Equal("سجل الأفكار المحدث", row.TitleAr);
        Assert.Equal("Ideas Register Updated", row.TitleEn);
        Assert.Equal(42, row.SortOrder);

        var auditRow = verifyDb.AuditLogs.SingleOrDefault(a => a.Action == "report_title.updated" && a.EntityId == ideasId);
        Assert.NotNull(auditRow);
    }

    [Fact]
    public async Task Put_UnknownId_NotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await client.PutAsJsonAsync($"/api/admin/report-titles/{Guid.NewGuid()}", new
        {
            titleAr = "أي شيء",
            titleEn = "Whatever",
            sortOrder = 1,
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SubmitterPutForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        Guid ideasId;
        using (var db = CreateDbContext())
        {
            ideasId = db.ReportTitles.Single(r => r.Key == "ideas").Id;
        }

        var response = await client.PutAsJsonAsync($"/api/admin/report-titles/{ideasId}", new
        {
            titleAr = "أي شيء",
            titleEn = "Whatever",
            sortOrder = 1,
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteRemovesReportTitle_AndAudits()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        Guid newId;
        using (var db = CreateDbContext())
        {
            var entity = new ReportTitle
            {
                Id = Guid.NewGuid(),
                Key = "to_delete",
                TitleAr = "للحذف",
                TitleEn = "To Delete",
                SortOrder = 500,
            };
            db.ReportTitles.Add(entity);
            db.SaveChanges();
            newId = entity.Id;
        }

        var response = await client.DeleteAsync($"/api/admin/report-titles/{newId}");

        Assert.True(response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.OK);

        using var verifyDb = CreateDbContext();
        Assert.Null(verifyDb.ReportTitles.SingleOrDefault(r => r.Id == newId));

        var auditRow = verifyDb.AuditLogs.SingleOrDefault(a => a.Action == "report_title.deleted" && a.EntityId == newId);
        Assert.NotNull(auditRow);
    }

    [Fact]
    public async Task Delete_UnknownId_NotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await client.DeleteAsync($"/api/admin/report-titles/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_SystemReportTypeTitle_BadRequest_AndTitleStillExists()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var getBeforeResponse = await client.GetAsync("/api/admin/report-titles");
        var getBeforeBody = JsonDocument.Parse(await getBeforeResponse.Content.ReadAsStringAsync()).RootElement;
        var executiveTitle = getBeforeBody.EnumerateArray().Single(i => i.GetProperty("key").GetString() == "executive");
        var executiveId = executiveTitle.GetProperty("id").GetGuid();

        var response = await client.DeleteAsync($"/api/admin/report-titles/{executiveId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var verifyDb = CreateDbContext();
        Assert.NotNull(verifyDb.ReportTitles.SingleOrDefault(r => r.Id == executiveId));

        var getAfterResponse = await client.GetAsync("/api/admin/report-titles");
        var getAfterBody = JsonDocument.Parse(await getAfterResponse.Content.ReadAsStringAsync()).RootElement;
        Assert.Contains(getAfterBody.EnumerateArray(), i => i.GetProperty("key").GetString() == "executive");
    }

    [Fact]
    public async Task SubmitterDeleteForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        Guid ideasId;
        using (var db = CreateDbContext())
        {
            ideasId = db.ReportTitles.Single(r => r.Key == "ideas").Id;
        }

        var response = await client.DeleteAsync($"/api/admin/report-titles/{ideasId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    public void Dispose() => _connection.Dispose();
}
