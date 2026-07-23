using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Reports.Bundle;
using InnovationToImpact.Infrastructure.Auth;
using InnovationToImpact.Infrastructure.Data;
using InnovationToImpact.Infrastructure.Reports;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace InnovationToImpact.Api.Tests;

public class ReportGenerateEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"reports-generate-test-{Guid.NewGuid():N}");

    public ReportGenerateEndpointTests(WebApplicationFactory<Program> factory)
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

                services.Configure<ReportStorageOptions>(options => options.RootPath = _rootPath);

                using var scope = services.BuildServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
                db.Database.EnsureCreated();
                SeedData(db);
            });
        });
    }

    private static void SeedData(InnovationDbContext db)
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

        var themeId = db.StrategicThemes.First().Id;
        var approvedStatusId = db.IdeaStatuses.Single(s => s.Code == "approved").Id;
        var submittedStatusId = db.IdeaStatuses.Single(s => s.Code == "submitted").Id;

        db.Ideas.Add(new Idea
        {
            Id = Guid.NewGuid(), Code = "IDEA-GEN-1", TitleAr = "فكرة أولى", TitleEn = "First Idea",
            ProblemStatementAr = "م", ProblemStatementEn = "P", ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ف", ExpectedBenefitsEn = "B", StrategicThemeId = themeId,
            IdeaStatusId = approvedStatusId, SubmitterId = submitterId,
        });
        db.Ideas.Add(new Idea
        {
            Id = Guid.NewGuid(), Code = "IDEA-GEN-2", TitleAr = "فكرة ثانية", TitleEn = "Second Idea",
            ProblemStatementAr = "م", ProblemStatementEn = "P", ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ف", ExpectedBenefitsEn = "B", StrategicThemeId = themeId,
            IdeaStatusId = submittedStatusId, SubmitterId = submitterId, CurrentStage = 2,
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task Generate_AsSupervisor_ForExecutiveType_ReturnsCompletedThenDownloadable()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.PostAsync("/api/admin/reports/generate?type=executive", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("reportGenerationId").GetGuid() != Guid.Empty);
        Assert.Equal("completed", body.GetProperty("status").GetString());
        var reportGenerationId = body.GetProperty("reportGenerationId").GetGuid();

        var downloadResponse = await client.GetAsync($"/api/admin/reports/{reportGenerationId}/download");
        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", downloadResponse.Content.Headers.ContentType?.MediaType);

        var bytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 0);
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        Assert.Contains(workbook.Worksheets, w => w.Name == "Summary");
    }

    [Fact]
    public async Task Generate_AllTwelveReportTypes_ReturnCompletedStatus()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        foreach (var type in ReportTypeCodes.All)
        {
            var response = await client.PostAsync($"/api/admin/reports/generate?type={type}", null);

            Assert.True(response.StatusCode == HttpStatusCode.OK, $"type={type} returned {response.StatusCode}");
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("completed", body.GetProperty("status").GetString());

            var reportGenerationId = body.GetProperty("reportGenerationId").GetGuid();
            var downloadResponse = await client.GetAsync($"/api/admin/reports/{reportGenerationId}/download");
            Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
            var bytes = await downloadResponse.Content.ReadAsByteArrayAsync();
            Assert.True(bytes.Length > 0, $"type={type} produced empty file");
        }
    }

    [Fact]
    public async Task Generate_UnknownType_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.PostAsync("/api/admin/reports/generate?type=nonsense", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Generate_PdfFormat_ReturnsCompletedThenDownloadablePdfBytes()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.PostAsync("/api/admin/reports/generate?type=executive&format=pdf", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("completed", body.GetProperty("status").GetString());
        var reportGenerationId = body.GetProperty("reportGenerationId").GetGuid();

        var downloadResponse = await client.GetAsync($"/api/admin/reports/{reportGenerationId}/download");
        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        Assert.Equal("application/pdf", downloadResponse.Content.Headers.ContentType?.MediaType);

        var bytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 5);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
    }

    [Fact]
    public async Task Generate_PptxFormat_ReturnsCompletedThenDownloadablePptxBytes()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.PostAsync("/api/admin/reports/generate?type=executive&format=pptx", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("completed", body.GetProperty("status").GetString());
        var reportGenerationId = body.GetProperty("reportGenerationId").GetGuid();

        var downloadResponse = await client.GetAsync($"/api/admin/reports/{reportGenerationId}/download");
        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.presentationml.presentation", downloadResponse.Content.Headers.ContentType?.MediaType);

        var bytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 1);
        Assert.Equal("PK", Encoding.ASCII.GetString(bytes, 0, 2));
    }

    [Fact]
    public async Task Generate_XlsxFormat_StillReturnsCompletedThenDownloadableXlsxBytes()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.PostAsync("/api/admin/reports/generate?type=executive&format=xlsx", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("completed", body.GetProperty("status").GetString());
        var reportGenerationId = body.GetProperty("reportGenerationId").GetGuid();

        var downloadResponse = await client.GetAsync($"/api/admin/reports/{reportGenerationId}/download");
        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", downloadResponse.Content.Headers.ContentType?.MediaType);

        var bytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 0);
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        Assert.Contains(workbook.Worksheets, w => w.Name == "Summary");
    }

    [Fact]
    public async Task Generate_UnknownFormat_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.PostAsync("/api/admin/reports/generate?type=executive&format=csv", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Generate_AsSubmitter_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.PostAsync("/api/admin/reports/generate?type=executive", null);

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
