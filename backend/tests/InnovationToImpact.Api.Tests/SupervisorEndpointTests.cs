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

public class SupervisorEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"evidence-storage-test-{Guid.NewGuid():N}");
    private Guid _themeId;
    private Guid _activityId;
    private Guid _evaluatorId;

    public SupervisorEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("submitter1", "Submitter One", "submitter1@gac-demo.sa", null, null, null),
            new AdIdentity("supervisor1", "Supervisor One", "supervisor1@gac-demo.sa", null, null, null),
            new AdIdentity("evaluator1", "Evaluator One", "evaluator1@gac-demo.sa", null, null, null),
            new AdIdentity("judge1", "Judge One", "judge1@gac-demo.sa", null, null, null),
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
                SeedUsersAndRoles(db);
            });
        });
    }

    private void SeedUsersAndRoles(InnovationDbContext db)
    {
        var roleIds = db.Roles.ToDictionary(r => r.Code, r => r.Id);

        var submitterId = Guid.NewGuid();
        db.Users.Add(new User { Id = submitterId, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "submitter1", FullNameEn = "submitter1" });
        var supervisorId = Guid.NewGuid();
        db.Users.Add(new User { Id = supervisorId, SamAccountName = "supervisor1", Email = "supervisor1@gac-demo.sa", FullNameAr = "supervisor1", FullNameEn = "supervisor1" });
        _evaluatorId = Guid.NewGuid();
        db.Users.Add(new User { Id = _evaluatorId, SamAccountName = "evaluator1", Email = "evaluator1@gac-demo.sa", FullNameAr = "evaluator1", FullNameEn = "evaluator1" });
        var judgeId = Guid.NewGuid();
        db.Users.Add(new User { Id = judgeId, SamAccountName = "judge1", Email = "judge1@gac-demo.sa", FullNameAr = "judge1", FullNameEn = "judge1" });
        db.SaveChanges();

        db.Set<UserRole>().Add(new UserRole { UserId = submitterId, RoleId = roleIds[RoleCodes.Submitter], IsPrimary = true });
        db.Set<UserRole>().Add(new UserRole { UserId = supervisorId, RoleId = roleIds[RoleCodes.Supervisor], IsPrimary = true });
        db.Set<UserRole>().Add(new UserRole { UserId = _evaluatorId, RoleId = roleIds[RoleCodes.Evaluator], IsPrimary = true });
        db.Set<UserRole>().Add(new UserRole { UserId = judgeId, RoleId = roleIds[RoleCodes.Judge], IsPrimary = true });
        db.SaveChanges();

        _themeId = db.StrategicThemes.First().Id;
        _activityId = Guid.NewGuid();
        db.Activities.Add(new Activity { Id = _activityId, NameAr = "فعالية", NameEn = "Activity", Type = "hackathon", Status = "open", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30), CreatedById = submitterId });
        db.SaveChanges();
    }

    private static object MakeIdeaBody(Guid strategicThemeId, Guid activityId) => new
    {
        titleAr = "فكرة", titleEn = "Idea", problemStatementAr = "مشكلة", problemStatementEn = "Problem",
        proposedSolutionAr = "حل", proposedSolutionEn = "Solution", expectedBenefitsAr = "فوائد", expectedBenefitsEn = "Benefits",
        strategicThemeId,
        activityId,
        challengeId = (Guid?)null,
        participationType = "individual",
        teamName = (string?)null,
        teamMembers = Array.Empty<object>(),
        ipAcknowledged = true,
        termsAgreed = true,
    };

    private static Guid ExtractIdFromLocation(HttpResponseMessage response)
    {
        var location = response.Headers.Location!.ToString();
        return Guid.Parse(location.Split('/').Last());
    }

    [Fact]
    public async Task FullFlow_ScreeningApproveThenFinalRanking_TransitionsIdeaToApproved()
    {
        var submitterClient = _factory.CreateClient();
        submitterClient.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var createResponse = await submitterClient.PostAsJsonAsync("/api/ideas", MakeIdeaBody(_themeId, _activityId));
        var ideaId = ExtractIdFromLocation(createResponse);

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "evidence.pdf");
        await submitterClient.PostAsync($"/api/ideas/{ideaId}/attachments", content);

        var submitResponse = await submitterClient.PostAsync($"/api/ideas/{ideaId}/submit", null);
        var submitBody = await submitResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"submitted\"", submitBody);

        var supervisorClient = _factory.CreateClient();
        supervisorClient.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var queueResponse = await supervisorClient.GetAsync("/api/screening/queue");
        Assert.Equal(HttpStatusCode.OK, queueResponse.StatusCode);
        var queueBody = await queueResponse.Content.ReadAsStringAsync();
        Assert.Contains(ideaId.ToString(), queueBody);

        var trackAssignResponse = await supervisorClient.PostAsJsonAsync("/api/track-assignments", new { evaluatorId = _evaluatorId, trackId = _themeId });
        Assert.Equal(HttpStatusCode.Created, trackAssignResponse.StatusCode);

        var usersResponse = await supervisorClient.GetAsync("/api/users?role=evaluator");
        Assert.Equal(HttpStatusCode.OK, usersResponse.StatusCode);
        var usersBody = await usersResponse.Content.ReadAsStringAsync();
        Assert.Contains("evaluator1", usersBody);

        var decisionResponse = await supervisorClient.PostAsJsonAsync($"/api/ideas/{ideaId}/screening-decision", new { decisionCode = "approve", reason = (string?)null });
        Assert.Equal(HttpStatusCode.OK, decisionResponse.StatusCode);
        var decisionBody = await decisionResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"evaluation\"", decisionBody);

        // Fast-forward directly to pending_final_ranking (the evaluator/judge/committee pipeline that
        // produces this transition is exercised end-to-end by EvaluationsEndpointTests and
        // CommitteeEndpointTests already; this test's purpose is the screening gate and final ranking,
        // not re-proving the middle of the pipeline).
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
            var idea = db.Ideas.Single(i => i.Id == ideaId);
            var rankingStatus = db.IdeaStatuses.Single(s => s.Code == "pending_final_ranking");
            idea.IdeaStatusId = rankingStatus.Id;
            idea.CommitteeFinalScore = 9m;
            db.SaveChanges();
        }

        var previewResponse = await supervisorClient.GetAsync("/api/final-ranking/preview");
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var previewBody = await previewResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"outcomeStatus\":\"approved\"", previewBody);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
            var reloaded = db.Ideas.Include(i => i.IdeaStatus).Single(i => i.Id == ideaId);
            Assert.Equal("pending_final_ranking", reloaded.IdeaStatus.Code);
        }

        var runResponse = await supervisorClient.PostAsync("/api/final-ranking/run", null);
        Assert.Equal(HttpStatusCode.OK, runResponse.StatusCode);
        var runBody = await runResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"approvedCount\":1", runBody);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
            var reloaded = db.Ideas.Include(i => i.IdeaStatus).Single(i => i.Id == ideaId);
            Assert.Equal("approved", reloaded.IdeaStatus.Code);
            Assert.Equal(1, reloaded.FinalRank);
        }
    }

    [Fact]
    public async Task ScreeningDecision_Reject_RequiresReason()
    {
        var submitterClient = _factory.CreateClient();
        submitterClient.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var createResponse = await submitterClient.PostAsJsonAsync("/api/ideas", MakeIdeaBody(_themeId, _activityId));
        var ideaId = ExtractIdFromLocation(createResponse);

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "evidence.pdf");
        await submitterClient.PostAsync($"/api/ideas/{ideaId}/attachments", content);
        await submitterClient.PostAsync($"/api/ideas/{ideaId}/submit", null);

        var supervisorClient = _factory.CreateClient();
        supervisorClient.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await supervisorClient.PostAsJsonAsync($"/api/ideas/{ideaId}/screening-decision", new { decisionCode = "reject", reason = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ScreeningDecision_WithEvaluatorRole_ReturnsForbidden()
    {
        var submitterClient = _factory.CreateClient();
        submitterClient.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var createResponse = await submitterClient.PostAsJsonAsync("/api/ideas", MakeIdeaBody(_themeId, _activityId));
        var ideaId = ExtractIdFromLocation(createResponse);

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "evidence.pdf");
        await submitterClient.PostAsync($"/api/ideas/{ideaId}/attachments", content);
        await submitterClient.PostAsync($"/api/ideas/{ideaId}/submit", null);

        var evaluatorClient = _factory.CreateClient();
        evaluatorClient.DefaultRequestHeaders.Add("X-Dev-User", "evaluator1");

        var response = await evaluatorClient.PostAsJsonAsync($"/api/ideas/{ideaId}/screening-decision", new { decisionCode = "approve", reason = (string?)null });

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
