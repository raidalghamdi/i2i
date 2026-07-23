using System.Net;
using System.Net.Http.Json;
using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Ideas;
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

public class CommitteeEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"evidence-storage-test-{Guid.NewGuid():N}");
    private Guid _themeId;
    private Guid _activityId;

    public CommitteeEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("submitter1", "Submitter One", "submitter1@gac-demo.sa", null, null, null),
            new AdIdentity("evaluator1", "Evaluator One", "evaluator1@gac-demo.sa", null, null, null),
            new AdIdentity("judge1", "Judge One", "judge1@gac-demo.sa", null, null, null),
            new AdIdentity("judge2", "Judge Two", "judge2@gac-demo.sa", null, null, null),
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
                SeedUsersRolesAndAssignment(db);
            });
        });
    }

    private void SeedUsersRolesAndAssignment(InnovationDbContext db)
    {
        var roleIds = db.Roles.ToDictionary(r => r.Code, r => r.Id);

        var submitterId = Guid.NewGuid();
        db.Users.Add(new User { Id = submitterId, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "submitter1", FullNameEn = "submitter1" });
        var evaluatorId = Guid.NewGuid();
        db.Users.Add(new User { Id = evaluatorId, SamAccountName = "evaluator1", Email = "evaluator1@gac-demo.sa", FullNameAr = "evaluator1", FullNameEn = "evaluator1" });
        var judge1Id = Guid.NewGuid();
        db.Users.Add(new User { Id = judge1Id, SamAccountName = "judge1", Email = "judge1@gac-demo.sa", FullNameAr = "judge1", FullNameEn = "judge1" });
        var judge2Id = Guid.NewGuid();
        db.Users.Add(new User { Id = judge2Id, SamAccountName = "judge2", Email = "judge2@gac-demo.sa", FullNameAr = "judge2", FullNameEn = "judge2" });
        db.SaveChanges();

        db.Set<UserRole>().Add(new UserRole { UserId = submitterId, RoleId = roleIds[RoleCodes.Submitter], IsPrimary = true });
        db.Set<UserRole>().Add(new UserRole { UserId = evaluatorId, RoleId = roleIds[RoleCodes.Evaluator], IsPrimary = true });
        db.Set<UserRole>().Add(new UserRole { UserId = judge1Id, RoleId = roleIds[RoleCodes.Judge], IsPrimary = true });
        db.Set<UserRole>().Add(new UserRole { UserId = judge2Id, RoleId = roleIds[RoleCodes.Judge], IsPrimary = true });
        db.SaveChanges();

        _themeId = db.StrategicThemes.First().Id;
        _activityId = Guid.NewGuid();
        db.Activities.Add(new Activity { Id = _activityId, NameAr = "فعالية", NameEn = "Activity", Type = "hackathon", Status = "open", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30), CreatedById = submitterId });
        db.SaveChanges();
        db.Set<EvaluatorTrackAssignment>().Add(new EvaluatorTrackAssignment { Id = Guid.NewGuid(), EvaluatorId = evaluatorId, TrackId = _themeId, AssignedById = evaluatorId });
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

    private static async Task UploadAttachmentAsync(HttpClient client, Guid ideaId, string fileName)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", fileName);
        var response = await client.PostAsync($"/api/ideas/{ideaId}/attachments", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<Guid> CreateIdeaThroughToPendingCommitteeAsync()
    {
        var submitterClient = _factory.CreateClient();
        submitterClient.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var createResponse = await submitterClient.PostAsJsonAsync("/api/ideas", MakeIdeaBody(_themeId, _activityId));
        var ideaId = ExtractIdFromLocation(createResponse);
        await UploadAttachmentAsync(submitterClient, ideaId, "evidence.pdf");
        var submitResponse = await submitterClient.PostAsync($"/api/ideas/{ideaId}/submit", null);
        var submitBody = await submitResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"submitted\"", submitBody);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
            var idea = db.Ideas.Single(i => i.Id == ideaId);
            var evaluationStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Evaluation);
            idea.IdeaStatusId = evaluationStatus.Id;
            db.SaveChanges();
        }

        var evaluatorClient = _factory.CreateClient();
        evaluatorClient.DefaultRequestHeaders.Add("X-Dev-User", "evaluator1");
        var evaluationBody = new { innovation = 8, impact = 8, execution = 8, scalability = 8, presentation = 8, comments = (string?)null };
        var evaluateResponse = await evaluatorClient.PostAsJsonAsync($"/api/ideas/{ideaId}/evaluations", evaluationBody);
        var evaluateBody = await evaluateResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"ideaStatus\":\"pass_awaiting_attachments\"", evaluateBody);

        await UploadAttachmentAsync(submitterClient, ideaId, "post-pass-evidence.pdf");
        var submitToCommitteeResponse = await submitterClient.PostAsync($"/api/ideas/{ideaId}/submit-to-committee", null);
        var submitToCommitteeBody = await submitToCommitteeResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"committee\"", submitToCommitteeBody);

        return ideaId;
    }

    [Fact]
    public async Task FullFlow_TwoJudgesDecide_TransitionsToPendingFinalRanking()
    {
        var ideaId = await CreateIdeaThroughToPendingCommitteeAsync();

        var judge1Client = _factory.CreateClient();
        judge1Client.DefaultRequestHeaders.Add("X-Dev-User", "judge1");

        var criteriaResponse = await judge1Client.GetAsync("/api/committee-criteria");
        Assert.Equal(HttpStatusCode.OK, criteriaResponse.StatusCode);
        var criteriaBody = await criteriaResponse.Content.ReadAsStringAsync();
        Assert.Contains("originality", criteriaBody);

        var queueResponse = await judge1Client.GetAsync("/api/committee/queue");
        Assert.Equal(HttpStatusCode.OK, queueResponse.StatusCode);
        var queueBody = await queueResponse.Content.ReadAsStringAsync();
        Assert.Contains(ideaId.ToString(), queueBody);
        Assert.Contains("\"totalJudges\":2", queueBody);

        var decisionBody1 = new { decisionTypeCode = "approved", criteriaScores = new Dictionary<string, decimal> { ["originality"] = 8, ["feasibility"] = 8, ["impact"] = 8, ["alignment"] = 8 }, comments = "Good." };
        var decideResponse1 = await judge1Client.PostAsJsonAsync($"/api/ideas/{ideaId}/committee-decisions", decisionBody1);
        Assert.Equal(HttpStatusCode.OK, decideResponse1.StatusCode);
        var decideBody1 = await decideResponse1.Content.ReadAsStringAsync();
        Assert.Contains("\"ideaStatus\":\"committee\"", decideBody1);

        var judge2Client = _factory.CreateClient();
        judge2Client.DefaultRequestHeaders.Add("X-Dev-User", "judge2");
        var decisionBody2 = new { decisionTypeCode = "approved", criteriaScores = new Dictionary<string, decimal> { ["originality"] = 6, ["feasibility"] = 6, ["impact"] = 6, ["alignment"] = 6 }, comments = (string?)null };
        var decideResponse2 = await judge2Client.PostAsJsonAsync($"/api/ideas/{ideaId}/committee-decisions", decisionBody2);
        Assert.Equal(HttpStatusCode.OK, decideResponse2.StatusCode);
        var decideBody2 = await decideResponse2.Content.ReadAsStringAsync();
        Assert.Contains("\"ideaStatus\":\"pending_final_ranking\"", decideBody2);

        var mineResponse = await judge2Client.GetAsync("/api/committee/mine");
        Assert.Equal(HttpStatusCode.OK, mineResponse.StatusCode);
        var mineBody = await mineResponse.Content.ReadAsStringAsync();
        Assert.Contains(ideaId.ToString(), mineBody);
    }

    [Fact]
    public async Task Decide_WithSubmitterRole_ReturnsForbidden()
    {
        var ideaId = await CreateIdeaThroughToPendingCommitteeAsync();

        var submitterClient = _factory.CreateClient();
        submitterClient.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");
        var decisionBody = new { decisionTypeCode = "approved", criteriaScores = new Dictionary<string, decimal> { ["originality"] = 8, ["feasibility"] = 8, ["impact"] = 8, ["alignment"] = 8 }, comments = (string?)null };
        var response = await submitterClient.PostAsJsonAsync($"/api/ideas/{ideaId}/committee-decisions", decisionBody);

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
