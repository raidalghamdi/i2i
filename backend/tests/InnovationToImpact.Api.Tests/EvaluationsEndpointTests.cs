using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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

public class EvaluationsEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"evidence-storage-test-{Guid.NewGuid():N}");
    private Guid _submitterId;
    private Guid _evaluatorId;
    private Guid _themeId;
    private Guid _activityId;

    public EvaluationsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("submitter1", "Submitter One", "submitter1@gac-demo.sa", null, null, null),
            new AdIdentity("evaluator1", "Evaluator One", "evaluator1@gac-demo.sa", null, null, null),
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

        _submitterId = Guid.NewGuid();
        db.Users.Add(new User { Id = _submitterId, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "submitter1", FullNameEn = "submitter1" });
        _evaluatorId = Guid.NewGuid();
        db.Users.Add(new User { Id = _evaluatorId, SamAccountName = "evaluator1", Email = "evaluator1@gac-demo.sa", FullNameAr = "evaluator1", FullNameEn = "evaluator1" });
        db.SaveChanges();

        db.Set<UserRole>().Add(new UserRole { UserId = _submitterId, RoleId = roleIds[RoleCodes.Submitter], IsPrimary = true });
        db.Set<UserRole>().Add(new UserRole { UserId = _evaluatorId, RoleId = roleIds[RoleCodes.Evaluator], IsPrimary = true });
        db.SaveChanges();

        _themeId = db.StrategicThemes.First().Id;
        _activityId = Guid.NewGuid();
        db.Activities.Add(new Activity { Id = _activityId, NameAr = "فعالية", NameEn = "Activity", Type = "hackathon", Status = "open", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30), CreatedById = _submitterId });
        db.SaveChanges();
        db.Set<EvaluatorTrackAssignment>().Add(new EvaluatorTrackAssignment { Id = Guid.NewGuid(), EvaluatorId = _evaluatorId, TrackId = _themeId, AssignedById = _evaluatorId });
        db.SaveChanges();
    }

    private static object MakeIdeaBody(Guid strategicThemeId, Guid activityId) => new
    {
        titleAr = "فكرة",
        titleEn = "Idea",
        problemStatementAr = "مشكلة",
        problemStatementEn = "Problem",
        proposedSolutionAr = "حل",
        proposedSolutionEn = "Solution",
        expectedBenefitsAr = "فوائد",
        expectedBenefitsEn = "Benefits",
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

    private async Task<Guid> CreateAttachAndSubmitIdeaAsync(HttpClient submitterClient)
    {
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

        // The supervisor slice's screening gate (a later plan) is what normally moves an idea from
        // "submitted" to "evaluation". This helper predates that gate and only exercises the evaluation
        // flow, so it simulates "screening already approved this idea" via a direct DB status write.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
            var idea = db.Ideas.Single(i => i.Id == ideaId);
            var evaluationStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Evaluation);
            idea.IdeaStatusId = evaluationStatus.Id;
            db.SaveChanges();
        }

        return ideaId;
    }

    [Fact]
    public async Task SubmitThenEvaluate_TransitionsIdeaAndAppearsInMyEvaluations()
    {
        var submitterClient = _factory.CreateClient();
        submitterClient.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");
        var ideaId = await CreateAttachAndSubmitIdeaAsync(submitterClient);

        var evaluatorClient = _factory.CreateClient();
        evaluatorClient.DefaultRequestHeaders.Add("X-Dev-User", "evaluator1");

        var queueResponse = await evaluatorClient.GetAsync("/api/evaluations/queue");
        Assert.Equal(HttpStatusCode.OK, queueResponse.StatusCode);
        var queueBody = await queueResponse.Content.ReadAsStringAsync();
        Assert.Contains(ideaId.ToString(), queueBody);

        var evaluationBody = new { innovation = 7, impact = 7, execution = 7, scalability = 7, presentation = 7, comments = "Good idea." };
        var evaluateResponse = await evaluatorClient.PostAsJsonAsync($"/api/ideas/{ideaId}/evaluations", evaluationBody);
        Assert.Equal(HttpStatusCode.OK, evaluateResponse.StatusCode);
        var evaluateBody = await evaluateResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"recommendation\":\"pass\"", evaluateBody);
        Assert.Contains("\"ideaStatus\":\"pass_awaiting_attachments\"", evaluateBody);

        var mineResponse = await evaluatorClient.GetAsync("/api/evaluations/mine");
        Assert.Equal(HttpStatusCode.OK, mineResponse.StatusCode);
        var mineBody = await mineResponse.Content.ReadAsStringAsync();
        Assert.Contains(ideaId.ToString(), mineBody);
    }

    [Fact]
    public async Task Evaluate_WithSubmitterRole_ReturnsForbidden()
    {
        var submitterClient = _factory.CreateClient();
        submitterClient.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");
        var ideaId = await CreateAttachAndSubmitIdeaAsync(submitterClient);

        var evaluationBody = new { innovation = 7, impact = 7, execution = 7, scalability = 7, presentation = 7, comments = (string?)null };
        var response = await submitterClient.PostAsJsonAsync($"/api/ideas/{ideaId}/evaluations", evaluationBody);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMine_ReturnsIdeaEnteredEvaluationAt_NullWhenNotSet()
    {
        var submitterClient = _factory.CreateClient();
        submitterClient.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        // Create idea with EnteredEvaluationAt set
        var ideaWithEntryId = await CreateAttachAndSubmitIdeaAsync(submitterClient);

        // Create idea without EnteredEvaluationAt set (null)
        var ideaWithoutEntryId = await CreateAttachAndSubmitIdeaAsync(submitterClient);

        // Set EnteredEvaluationAt for the first idea only
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
            var ideaWithEntry = db.Ideas.Single(i => i.Id == ideaWithEntryId);
            ideaWithEntry.EnteredEvaluationAt = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
            // ideaWithoutEntry.EnteredEvaluationAt remains null
            db.SaveChanges();
        }

        // Create evaluations for both ideas
        var evaluatorClient = _factory.CreateClient();
        evaluatorClient.DefaultRequestHeaders.Add("X-Dev-User", "evaluator1");

        var evaluationBody = new { innovation = 7, impact = 7, execution = 7, scalability = 7, presentation = 7, comments = "Good idea." };
        await evaluatorClient.PostAsJsonAsync($"/api/ideas/{ideaWithEntryId}/evaluations", evaluationBody);
        await evaluatorClient.PostAsJsonAsync($"/api/ideas/{ideaWithoutEntryId}/evaluations", evaluationBody);

        // Get evaluations and verify ideaEnteredEvaluationAt field
        var response = await evaluatorClient.GetAsync("/api/evaluations/mine");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var items = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.EnumerateArray().ToList();
        Assert.Equal(2, items.Count);

        var withEntry = items.Single(i => i.GetProperty("ideaId").GetGuid() == ideaWithEntryId);
        Assert.False(withEntry.GetProperty("ideaEnteredEvaluationAt").ValueKind == JsonValueKind.Null);
        Assert.Equal(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), withEntry.GetProperty("ideaEnteredEvaluationAt").GetDateTime());

        var withoutEntry = items.Single(i => i.GetProperty("ideaId").GetGuid() == ideaWithoutEntryId);
        Assert.True(withoutEntry.GetProperty("ideaEnteredEvaluationAt").ValueKind == JsonValueKind.Null);
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
