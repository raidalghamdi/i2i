using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Entities;
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

public class AnalyticsExportEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"analytics-export-test-{Guid.NewGuid():N}");

    public AnalyticsExportEndpointTests(WebApplicationFactory<Program> factory)
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
        var draftStatusId = db.IdeaStatuses.Single(s => s.Code == "draft").Id;
        var submittedStatusId = db.IdeaStatuses.Single(s => s.Code == "submitted").Id;

        db.Ideas.Add(new Idea
        {
            Id = Guid.NewGuid(), Code = "IDEA-EXP-1", TitleAr = "فكرة أولى", TitleEn = "First Idea",
            ProblemStatementAr = "م", ProblemStatementEn = "P", ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ف", ExpectedBenefitsEn = "B", StrategicThemeId = themeId,
            IdeaStatusId = draftStatusId, SubmitterId = submitterId,
        });
        db.Ideas.Add(new Idea
        {
            Id = Guid.NewGuid(), Code = "IDEA-EXP-2", TitleAr = "فكرة ثانية", TitleEn = "Second Idea",
            ProblemStatementAr = "م", ProblemStatementEn = "P", ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ف", ExpectedBenefitsEn = "B", StrategicThemeId = themeId,
            IdeaStatusId = submittedStatusId, SubmitterId = submitterId, CurrentStage = 2,
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task Export_AsSupervisor_ReturnsCompletedStatusWithReportGenerationId()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.PostAsync("/api/admin/analytics/export?format=xlsx", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("reportGenerationId").GetGuid() != Guid.Empty);
        Assert.Equal("completed", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Export_ThenDownload_ReturnsMultiSheetWorkbookWithExpectedSheetNames()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var generateResponse = await client.PostAsync("/api/admin/analytics/export?format=xlsx", null);
        Assert.Equal(HttpStatusCode.OK, generateResponse.StatusCode);
        var generateBody = await generateResponse.Content.ReadFromJsonAsync<JsonElement>();
        var reportGenerationId = generateBody.GetProperty("reportGenerationId").GetGuid();

        var downloadResponse = await client.GetAsync($"/api/admin/reports/{reportGenerationId}/download");

        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", downloadResponse.Content.Headers.ContentType?.MediaType);
        var bytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 0);

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        Assert.Equal(
            new[] { "KPIs", "Funnel", "Cohort", "IdeasByStage", "TopObjectives", "AvgTimePerStage", "Conversion" },
            workbook.Worksheets.Select(w => w.Name));
    }

    [Fact]
    public async Task Export_PdfFormat_ReturnsCompletedStatusWithReportGenerationId()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.PostAsync("/api/admin/analytics/export?format=pdf", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("reportGenerationId").GetGuid() != Guid.Empty);
        Assert.Equal("completed", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Export_PdfFormat_ThenDownload_ReturnsPdfDocument()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var generateResponse = await client.PostAsync("/api/admin/analytics/export?format=pdf", null);
        Assert.Equal(HttpStatusCode.OK, generateResponse.StatusCode);
        var generateBody = await generateResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("completed", generateBody.GetProperty("status").GetString());
        var reportGenerationId = generateBody.GetProperty("reportGenerationId").GetGuid();

        var downloadResponse = await client.GetAsync($"/api/admin/reports/{reportGenerationId}/download");

        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        Assert.Equal("application/pdf", downloadResponse.Content.Headers.ContentType?.MediaType);
        var bytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 0);

        // PDF magic bytes: %PDF
        Assert.True(bytes.Length >= 4);
        Assert.Equal(0x25, bytes[0]); // %
        Assert.Equal(0x50, bytes[1]); // P
        Assert.Equal(0x44, bytes[2]); // D
        Assert.Equal(0x46, bytes[3]); // F
    }

    [Fact]
    public async Task Export_PptxFormat_ReturnsCompletedStatusWithReportGenerationId()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.PostAsync("/api/admin/analytics/export?format=pptx", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("reportGenerationId").GetGuid() != Guid.Empty);
        Assert.Equal("completed", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Export_PptxFormat_ThenDownload_ReturnsValidPresentationDocument()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var generateResponse = await client.PostAsync("/api/admin/analytics/export?format=pptx", null);
        Assert.Equal(HttpStatusCode.OK, generateResponse.StatusCode);
        var generateBody = await generateResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("completed", generateBody.GetProperty("status").GetString());
        var reportGenerationId = generateBody.GetProperty("reportGenerationId").GetGuid();

        var downloadResponse = await client.GetAsync($"/api/admin/reports/{reportGenerationId}/download");

        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            downloadResponse.Content.Headers.ContentType?.MediaType);
        var bytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 0);

        // ZIP/OpenXml container magic bytes: PK
        Assert.True(bytes.Length >= 2);
        Assert.Equal(0x50, bytes[0]); // P
        Assert.Equal(0x4B, bytes[1]); // K

        using var stream = new MemoryStream(bytes);
        using var presentation = PresentationDocument.Open(stream, isEditable: false);
        var slideIdList = presentation.PresentationPart?.Presentation?.SlideIdList;
        Assert.NotNull(slideIdList);
        Assert.True(slideIdList!.ChildElements.Count >= 1);
    }

    [Fact]
    public async Task Export_InvalidFormat_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await client.PostAsync("/api/admin/analytics/export?format=foo", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Export_AsSubmitter_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.PostAsync("/api/admin/analytics/export?format=xlsx", null);

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
