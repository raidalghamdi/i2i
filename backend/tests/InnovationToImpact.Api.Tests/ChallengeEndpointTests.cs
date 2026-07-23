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

public class ChallengeEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private Guid _themeId;

    public ChallengeEndpointTests(WebApplicationFactory<Program> factory)
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
                Seed(db);
            });
        });
    }

    private void Seed(InnovationDbContext db)
    {
        var adminRoleId = db.Roles.Single(r => r.Code == "admin").Id;
        var submitterRoleId = db.Roles.Single(r => r.Code == "submitter").Id;

        var adminId = Guid.NewGuid();
        db.Users.Add(new User { Id = adminId, SamAccountName = "admin1", Email = "admin1@gac-demo.sa", FullNameAr = "a1", FullNameEn = "a1" });
        var submitterId = Guid.NewGuid();
        db.Users.Add(new User { Id = submitterId, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "s1", FullNameEn = "s1" });
        db.SaveChanges();

        db.Set<UserRole>().Add(new UserRole { UserId = adminId, RoleId = adminRoleId, IsPrimary = true });
        db.Set<UserRole>().Add(new UserRole { UserId = submitterId, RoleId = submitterRoleId, IsPrimary = true });
        db.SaveChanges();

        _themeId = db.StrategicThemes.First().Id;
    }

    [Fact]
    public async Task Create_AsAdmin_Succeeds()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await client.PostAsJsonAsync("/api/admin/challenges", new { strategicThemeId = _themeId, textAr = "تحدٍ", textEn = "Challenge", sortOrder = 0, isActive = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Create_AsSubmitter_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.PostAsJsonAsync("/api/admin/challenges", new { strategicThemeId = _themeId, textAr = "تحدٍ", textEn = "Challenge", sortOrder = 0, isActive = true });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PublicList_OnlyReturnsActiveChallengesForTheGivenTheme()
    {
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Add("X-Dev-User", "admin1");
        await adminClient.PostAsJsonAsync("/api/admin/challenges", new { strategicThemeId = _themeId, textAr = "أ", textEn = "Active", sortOrder = 0, isActive = true });
        await adminClient.PostAsJsonAsync("/api/admin/challenges", new { strategicThemeId = _themeId, textAr = "ب", textEn = "Inactive", sortOrder = 1, isActive = false });

        var submitterClient = _factory.CreateClient();
        submitterClient.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");
        var response = await submitterClient.GetAsync($"/api/challenges?themeId={_themeId}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, body.GetArrayLength());
        Assert.Equal("Active", body[0].GetProperty("textEn").GetString());
    }

    [Fact]
    public async Task Delete_AsAdmin_RemovesChallenge()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");
        var createResponse = await client.PostAsJsonAsync("/api/admin/challenges", new { strategicThemeId = _themeId, textAr = "أ", textEn = "A", sortOrder = 0, isActive = true });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();

        var deleteResponse = await client.DeleteAsync($"/api/admin/challenges/{id}");
        var getResponse = await client.GetAsync($"/api/admin/challenges/{id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_ChallengeInUse_ReturnsConflict()
    {
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Add("X-Dev-User", "admin1");
        var createResponse = await adminClient.PostAsJsonAsync("/api/admin/challenges", new { strategicThemeId = _themeId, textAr = "أ", textEn = "A", sortOrder = 0, isActive = true });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var challengeId = created.GetProperty("id").GetGuid();

        var activityId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
            var submitterId = db.Users.Single(u => u.SamAccountName == "submitter1").Id;
            db.Activities.Add(new Activity { Id = activityId, NameAr = "فعالية", NameEn = "Activity", Type = "hackathon", Status = "open", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30), CreatedById = submitterId });
            db.SaveChanges();
        }

        var submitterClient = _factory.CreateClient();
        submitterClient.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");
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
            strategicThemeId = _themeId,
            activityId,
            challengeId = (Guid?)challengeId,
            participationType = "individual",
            teamName = (string?)null,
            teamMembers = Array.Empty<object>(),
            ipAcknowledged = true,
            termsAgreed = true,
        };
        var ideaResponse = await submitterClient.PostAsJsonAsync("/api/ideas", ideaBody);
        Assert.Equal(HttpStatusCode.Created, ideaResponse.StatusCode);

        var deleteResponse = await adminClient.DeleteAsync($"/api/admin/challenges/{challengeId}");
        var getResponse = await adminClient.GetAsync($"/api/admin/challenges/{challengeId}");

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    public void Dispose() => _connection.Dispose();
}
