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

public class StrategicThemesEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;

    public StrategicThemesEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("submitter1", "Submitter One", "submitter1@gac-demo.sa", null, null, null),
            new AdIdentity("supervisor1", "Supervisor One", "supervisor1@gac-demo.sa", null, null, null),
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

                var roleIds = db.Roles.ToDictionary(r => r.Code, r => r.Id);
                var submitterId = Guid.NewGuid();
                db.Users.Add(new User { Id = submitterId, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "submitter1", FullNameEn = "submitter1" });
                var supervisorId = Guid.NewGuid();
                db.Users.Add(new User { Id = supervisorId, SamAccountName = "supervisor1", Email = "supervisor1@gac-demo.sa", FullNameAr = "supervisor1", FullNameEn = "supervisor1" });
                db.SaveChanges();
                db.Set<UserRole>().Add(new UserRole { UserId = submitterId, RoleId = roleIds[RoleCodes.Submitter], IsPrimary = true });
                db.Set<UserRole>().Add(new UserRole { UserId = supervisorId, RoleId = roleIds[RoleCodes.Supervisor], IsPrimary = true });
                db.SaveChanges();
            });
        });
    }

    [Fact]
    public async Task ReturnsSeededThemes()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.GetAsync("/api/strategic-themes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Digital Transformation", body);
        Assert.Contains("Customer Experience", body);
        Assert.Contains("Operational Efficiency", body);
    }

    [Fact]
    public async Task ReturnsSeededThemesWithDescriptionFields()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.GetAsync("/api/strategic-themes");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body[0].TryGetProperty("descriptionAr", out _));
        Assert.True(body[0].TryGetProperty("descriptionEn", out _));
    }

    [Fact]
    public async Task Create_AsSupervisor_Succeeds()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.PostAsJsonAsync("/api/strategic-themes", new { nameAr = "مسار جديد", nameEn = "New Track", descriptionAr = "وصف", descriptionEn = "Description" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_AsSubmitter_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.PostAsJsonAsync("/api/strategic-themes", new { nameAr = "مسار جديد", nameEn = "New Track", descriptionAr = (string?)null, descriptionEn = (string?)null });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ThemeInUse_ReturnsConflict()
    {
        var supervisorClient = _factory.CreateClient();
        supervisorClient.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");
        var createResponse = await supervisorClient.PostAsJsonAsync("/api/strategic-themes", new { nameAr = "أ", nameEn = "A", descriptionAr = (string?)null, descriptionEn = (string?)null });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var themeId = created.GetProperty("id").GetGuid();

        var submitterClient = _factory.CreateClient();
        submitterClient.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");
        var activityId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
            var submitterId = db.Users.Single(u => u.SamAccountName == "submitter1").Id;
            db.Activities.Add(new Activity { Id = activityId, NameAr = "فعالية", NameEn = "Activity", Type = "hackathon", Status = "open", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30), CreatedById = submitterId });
            db.SaveChanges();
        }
        var ideaBody = new
        {
            titleAr = "فكرة",
            titleEn = "Idea",
            problemStatementAr = "مشكلة",
            problemStatementEn = "Problem",
            proposedSolutionAr = "حل",
            proposedSolutionEn = "Solution",
            expectedBenefitsAr = "فوائد",
            expectedBenefitsEn = "Benefits",
            strategicThemeId = themeId,
            activityId,
            challengeId = (Guid?)null,
            participationType = "individual",
            teamName = (string?)null,
            teamMembers = Array.Empty<object>(),
            ipAcknowledged = true,
            termsAgreed = true,
        };
        var ideaResponse = await submitterClient.PostAsJsonAsync("/api/ideas", ideaBody);
        Assert.Equal(HttpStatusCode.Created, ideaResponse.StatusCode);

        var deleteResponse = await supervisorClient.DeleteAsync($"/api/strategic-themes/{themeId}");

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
    }

    public void Dispose() => _connection.Dispose();
}
