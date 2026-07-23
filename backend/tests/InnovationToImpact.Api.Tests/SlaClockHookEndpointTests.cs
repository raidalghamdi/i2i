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

public class SlaClockHookEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"sla-clock-hook-test-{Guid.NewGuid():N}");

    public SlaClockHookEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var lookup = new FakeAdIdentityLookupService(
            seedIdentities: new[]
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
                SeedUsers(db);
            });
        });
    }

    private static Guid _themeId;
    private static Guid _activityId;
    private static Guid _trackAssignmentEvaluatorId;

    private static void SeedUsers(InnovationDbContext db)
    {
        var submitterRoleId = db.Roles.Single(r => r.Code == "submitter").Id;
        var supervisorRoleId = db.Roles.Single(r => r.Code == "supervisor").Id;
        var evaluatorRoleId = db.Roles.Single(r => r.Code == "evaluator").Id;
        var judgeRoleId = db.Roles.Single(r => r.Code == "judge").Id;

        var submitterId = Guid.NewGuid();
        db.Users.Add(new User { Id = submitterId, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "submitter1", FullNameEn = "submitter1" });
        var supervisorId = Guid.NewGuid();
        db.Users.Add(new User { Id = supervisorId, SamAccountName = "supervisor1", Email = "supervisor1@gac-demo.sa", FullNameAr = "supervisor1", FullNameEn = "supervisor1" });
        var evaluatorId = Guid.NewGuid();
        db.Users.Add(new User { Id = evaluatorId, SamAccountName = "evaluator1", Email = "evaluator1@gac-demo.sa", FullNameAr = "evaluator1", FullNameEn = "evaluator1" });
        var judgeId = Guid.NewGuid();
        db.Users.Add(new User { Id = judgeId, SamAccountName = "judge1", Email = "judge1@gac-demo.sa", FullNameAr = "judge1", FullNameEn = "judge1" });
        db.SaveChanges();

        db.Set<UserRole>().AddRange(
            new UserRole { UserId = submitterId, RoleId = submitterRoleId, IsPrimary = true },
            new UserRole { UserId = supervisorId, RoleId = supervisorRoleId, IsPrimary = true },
            new UserRole { UserId = evaluatorId, RoleId = evaluatorRoleId, IsPrimary = true },
            new UserRole { UserId = judgeId, RoleId = judgeRoleId, IsPrimary = true });
        db.SaveChanges();

        _themeId = db.StrategicThemes.First().Id;
        _activityId = Guid.NewGuid();
        db.Activities.Add(new Activity { Id = _activityId, NameAr = "فعالية", NameEn = "Activity", Type = "hackathon", Status = "open", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30), CreatedById = submitterId });
        db.SaveChanges();

        db.EvaluatorTrackAssignments.Add(new EvaluatorTrackAssignment { Id = Guid.NewGuid(), EvaluatorId = evaluatorId, TrackId = _themeId, AssignedById = supervisorId });
        db.SaveChanges();
        _trackAssignmentEvaluatorId = evaluatorId;
    }

    [Fact]
    public async Task FullIdeaLifecycle_OpensAndClosesBothSlaClocksAtEachTransition()
    {
        var submitterClient = _factory.CreateClient();
        submitterClient.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");
        var supervisorClient = _factory.CreateClient();
        supervisorClient.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");
        var evaluatorClient = _factory.CreateClient();
        evaluatorClient.DefaultRequestHeaders.Add("X-Dev-User", "evaluator1");
        var judgeClient = _factory.CreateClient();
        judgeClient.DefaultRequestHeaders.Add("X-Dev-User", "judge1");

        var createResponse = await submitterClient.PostAsJsonAsync("/api/ideas", new
        {
            titleAr = "أ", titleEn = "T", problemStatementAr = "م", problemStatementEn = "P",
            proposedSolutionAr = "ح", proposedSolutionEn = "S", expectedBenefitsAr = "ف", expectedBenefitsEn = "B",
            strategicThemeId = _themeId,
            activityId = _activityId,
            challengeId = (Guid?)null,
            participationType = "individual",
            teamName = (string?)null,
            teamMembers = Array.Empty<object>(),
            ipAcknowledged = true,
            termsAgreed = true,
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var ideaId = created.GetProperty("id").GetGuid();

        using (var content = new MultipartFormDataContent())
        {
            var fileContent = new ByteArrayContent(new byte[] { 1 });
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            content.Add(fileContent, "file", "a.pdf");
            await submitterClient.PostAsync($"/api/ideas/{ideaId}/attachments", content);
        }
        await submitterClient.PostAsync($"/api/ideas/{ideaId}/submit", null);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
            Assert.Empty(db.SlaTrackings.ToList());
        }

        await supervisorClient.PostAsJsonAsync($"/api/ideas/{ideaId}/screening-decision", new { decisionCode = "approve", reason = (string?)null });

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
            var tracking = db.SlaTrackings.Include(t => t.SlaPolicy).Single(t => t.SlaPolicy.EntityType == "evaluation");
            Assert.Equal(ideaId, tracking.EntityId);
            Assert.Null(tracking.ResolvedAt);
        }

        await evaluatorClient.PostAsJsonAsync($"/api/ideas/{ideaId}/evaluations", new { innovation = 8, impact = 8, execution = 8, scalability = 8, presentation = 8, comments = (string?)null });

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
            var tracking = db.SlaTrackings.Include(t => t.SlaPolicy).Single(t => t.SlaPolicy.EntityType == "evaluation");
            Assert.NotNull(tracking.ResolvedAt);
        }

        using (var content = new MultipartFormDataContent())
        {
            var fileContent = new ByteArrayContent(new byte[] { 1 });
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            content.Add(fileContent, "file", "b.pdf");
            await submitterClient.PostAsync($"/api/ideas/{ideaId}/attachments", content);
        }
        await submitterClient.PostAsync($"/api/ideas/{ideaId}/submit-to-committee", null);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
            var tracking = db.SlaTrackings.Include(t => t.SlaPolicy).Single(t => t.SlaPolicy.EntityType == "committee");
            Assert.Equal(ideaId, tracking.EntityId);
            Assert.Null(tracking.ResolvedAt);
        }

        var criteriaScores = new Dictionary<string, decimal>
        {
            ["originality"] = 8,
            ["feasibility"] = 8,
            ["impact"] = 8,
            ["alignment"] = 8,
        };
        await judgeClient.PostAsJsonAsync($"/api/ideas/{ideaId}/committee-decisions", new { decisionTypeCode = "approved", comments = (string?)null, criteriaScores });

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
            var tracking = db.SlaTrackings.Include(t => t.SlaPolicy).Single(t => t.SlaPolicy.EntityType == "committee");
            Assert.NotNull(tracking.ResolvedAt);
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
        if (Directory.Exists(_rootPath)) Directory.Delete(_rootPath, recursive: true);
    }
}
