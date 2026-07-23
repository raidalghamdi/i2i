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

public class AnalyticsEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"analytics-endpoint-test-{Guid.NewGuid():N}");

    public AnalyticsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var lookup = new FakeAdIdentityLookupService(
            seedIdentities: new[]
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

                services.Configure<EvidenceStorageOptions>(options => options.RootPath = _rootPath);

                using var scope = services.BuildServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
                db.Database.EnsureCreated();

                var adminRoleId = db.Roles.Single(r => r.Code == "admin").Id;
                var adminId = Guid.NewGuid();
                db.Users.Add(new User { Id = adminId, SamAccountName = "admin1", Email = "admin1@gac-demo.sa", FullNameAr = "admin1", FullNameEn = "admin1" });
                var supervisorRoleId = db.Roles.Single(r => r.Code == "supervisor").Id;
                var supervisorId = Guid.NewGuid();
                db.Users.Add(new User { Id = supervisorId, SamAccountName = "supervisor1", Email = "supervisor1@gac-demo.sa", FullNameAr = "supervisor1", FullNameEn = "supervisor1" });
                var submitterRoleId = db.Roles.Single(r => r.Code == "submitter").Id;
                var submitterId = Guid.NewGuid();
                db.Users.Add(new User { Id = submitterId, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "submitter1", FullNameEn = "submitter1" });
                db.SaveChanges();
                db.Set<UserRole>().AddRange(
                    new UserRole { UserId = adminId, RoleId = adminRoleId, IsPrimary = true },
                    new UserRole { UserId = supervisorId, RoleId = supervisorRoleId, IsPrimary = true },
                    new UserRole { UserId = submitterId, RoleId = submitterRoleId, IsPrimary = true });
                db.SaveChanges();

                var themeId = db.StrategicThemes.First().Id;
                var statusId = db.IdeaStatuses.Single(s => s.Code == "draft").Id;
                db.Ideas.Add(new Idea
                {
                    Id = Guid.NewGuid(),
                    Code = "ANALYTICS-TEST-1",
                    TitleAr = "ا", TitleEn = "T", ProblemStatementAr = "م", ProblemStatementEn = "P",
                    ProposedSolutionAr = "ح", ProposedSolutionEn = "S", ExpectedBenefitsAr = "ف", ExpectedBenefitsEn = "B",
                    StrategicThemeId = themeId, IdeaStatusId = statusId, SubmitterId = submitterId,
                });
                db.SaveChanges();
            });
        });
    }

    [Fact]
    public async Task GetAnalytics_AsAdmin_ReturnsAllSixSections()
    {
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await adminClient.GetAsync("/api/admin/analytics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("platformKpis", out _));
        Assert.True(body.TryGetProperty("ideasByStatus", out _));
        Assert.True(body.TryGetProperty("submissionsOverTime", out _));
        Assert.True(body.TryGetProperty("themeActivity", out _));
        Assert.True(body.TryGetProperty("topEvaluators", out _));
        Assert.True(body.TryGetProperty("slaCompliance", out _));
        Assert.Equal(1, body.GetProperty("platformKpis").GetProperty("totalIdeas").GetInt32());
    }

    [Fact]
    public async Task GetAnalytics_AsNonAdmin_ReturnsForbidden()
    {
        var submitterClient = _factory.CreateClient();
        submitterClient.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await submitterClient.GetAsync("/api/admin/analytics");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_AsSupervisor_Succeeds()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.GetAsync("/api/admin/analytics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_AsSubmitter_Forbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.GetAsync("/api/admin/analytics");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    public void Dispose()
    {
        _connection.Dispose();
        if (Directory.Exists(_rootPath)) Directory.Delete(_rootPath, recursive: true);
    }
}
