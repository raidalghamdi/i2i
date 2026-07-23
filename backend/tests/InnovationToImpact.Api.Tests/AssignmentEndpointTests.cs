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

public class AssignmentEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    protected Guid IdeaId;
    protected Guid EvaluatorId;

    public AssignmentEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("supervisor1", "Supervisor One", "supervisor1@gac-demo.sa", null, null, null),
            new AdIdentity("evaluator1", "Evaluator One", "evaluator1@gac-demo.sa", null, null, null),
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

                var roleIds = db.Roles.ToDictionary(r => r.Code, r => r.Id);
                var supervisorId = Guid.NewGuid();
                db.Users.Add(new User { Id = supervisorId, SamAccountName = "supervisor1", Email = "supervisor1@gac-demo.sa", FullNameAr = "s1", FullNameEn = "s1" });
                EvaluatorId = Guid.NewGuid();
                db.Users.Add(new User { Id = EvaluatorId, SamAccountName = "evaluator1", Email = "evaluator1@gac-demo.sa", FullNameAr = "e1", FullNameEn = "Evaluator One" });
                var submitterId = Guid.NewGuid();
                db.Users.Add(new User { Id = submitterId, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "sub1", FullNameEn = "sub1" });
                db.SaveChanges();
                db.Set<UserRole>().Add(new UserRole { UserId = supervisorId, RoleId = roleIds[RoleCodes.Supervisor], IsPrimary = true });
                db.Set<UserRole>().Add(new UserRole { UserId = EvaluatorId, RoleId = roleIds[RoleCodes.Evaluator], IsPrimary = true });
                db.Set<UserRole>().Add(new UserRole { UserId = submitterId, RoleId = roleIds[RoleCodes.Submitter], IsPrimary = true });
                db.SaveChanges();

                var activityId = Guid.NewGuid();
                db.Activities.Add(new Activity { Id = activityId, NameAr = "ف", NameEn = "Activity", Type = "hackathon", Status = "open", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30), CreatedById = submitterId });
                var themeId = db.StrategicThemes.First().Id;
                var draftStatus = db.IdeaStatuses.Single(s => s.Code == "draft");
                IdeaId = Guid.NewGuid();
                db.Ideas.Add(new Idea
                {
                    Id = IdeaId,
                    Code = "IDEA-0001",
                    TitleAr = "ا", TitleEn = "T",
                    ProblemStatementAr = "م", ProblemStatementEn = "P",
                    ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
                    ExpectedBenefitsAr = "ف", ExpectedBenefitsEn = "B",
                    StrategicThemeId = themeId,
                    ActivityId = activityId,
                    IdeaStatusId = draftStatus.Id,
                    SubmitterId = submitterId,
                });
                db.SaveChanges();
            });
        });
    }

    [Fact]
    public async Task List_AsSupervisor_ReturnsPagedResult()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.GetAsync("/api/admin/assignments?page=1&pageSize=25");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, body.GetProperty("total").GetInt32());
        Assert.Equal(0, body.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task List_AsEvaluator_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "evaluator1");

        var response = await client.GetAsync("/api/admin/assignments?page=1&pageSize=25");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task WorkloadHeatmap_AsSupervisor_ReturnsEmptyArrayWhenNoAssignments()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.GetAsync("/api/admin/assignments/workload-heatmap");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, body.GetArrayLength());
    }

    [Fact]
    public async Task SuggestEvaluators_AsSupervisor_ReturnsTheSeededEvaluator()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.GetAsync("/api/admin/assignments/suggest-evaluators");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, body.GetArrayLength());
        Assert.Equal("Evaluator One", body[0].GetProperty("evaluatorName").GetString());
    }

    [Fact]
    public async Task IdeaOptions_AsSupervisor_ReturnsSeededIdea()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.GetAsync("/api/admin/assignments/idea-options");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, body.GetArrayLength());
        Assert.Equal("IDEA-0001", body[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task Create_AsSupervisor_Succeeds_AndOpensSlaTracker()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.PostAsJsonAsync("/api/admin/assignments", new { ideaId = IdeaId, evaluatorId = EvaluatorId, dueAt = (DateTime?)null, notes = "please review" });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var assignmentId = body.GetProperty("id").GetGuid();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
        var policyId = db.SlaPolicies.Single(p => p.EntityType == "assignment").Id;
        Assert.True(db.SlaTrackings.Any(t => t.SlaPolicyId == policyId && t.EntityId == assignmentId && t.ResolvedAt == null));
    }

    [Fact]
    public async Task Create_InvalidIdea_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.PostAsJsonAsync("/api/admin/assignments", new { ideaId = Guid.NewGuid(), evaluatorId = EvaluatorId, dueAt = (DateTime?)null, notes = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ToCompleted_ClosesSlaTracker()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");
        var createResponse = await client.PostAsJsonAsync("/api/admin/assignments", new { ideaId = IdeaId, evaluatorId = EvaluatorId, dueAt = (DateTime?)null, notes = (string?)null });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var assignmentId = created.GetProperty("id").GetGuid();

        var updateResponse = await client.PatchAsJsonAsync($"/api/admin/assignments/{assignmentId}", new { statusCode = "completed", dueAt = (DateTime?)null, notes = (string?)null, evaluatorId = EvaluatorId });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
        var policyId = db.SlaPolicies.Single(p => p.EntityType == "assignment").Id;
        Assert.False(db.SlaTrackings.Any(t => t.SlaPolicyId == policyId && t.EntityId == assignmentId && t.ResolvedAt == null));
    }

    [Fact]
    public async Task Unassign_AsSupervisor_ClosesSlaTracker()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");
        var createResponse = await client.PostAsJsonAsync("/api/admin/assignments", new { ideaId = IdeaId, evaluatorId = EvaluatorId, dueAt = (DateTime?)null, notes = (string?)null });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var assignmentId = created.GetProperty("id").GetGuid();

        var deleteResponse = await client.DeleteAsync($"/api/admin/assignments/{assignmentId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
        var policyId = db.SlaPolicies.Single(p => p.EntityType == "assignment").Id;
        Assert.False(db.SlaTrackings.Any(t => t.SlaPolicyId == policyId && t.EntityId == assignmentId && t.ResolvedAt == null));
    }

    [Fact]
    public async Task BulkUnassign_AsSupervisor_ClosesAllSlaTrackers()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");
        var create1 = await client.PostAsJsonAsync("/api/admin/assignments", new { ideaId = IdeaId, evaluatorId = EvaluatorId, dueAt = (DateTime?)null, notes = (string?)null });
        var created1 = await create1.Content.ReadFromJsonAsync<JsonElement>();
        var id1 = created1.GetProperty("id").GetGuid();

        var bulkResponse = await client.PostAsJsonAsync("/api/admin/assignments/bulk-unassign", new { ids = new[] { id1 } });
        var body = await bulkResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, bulkResponse.StatusCode);
        Assert.Equal(1, body.GetProperty("unassigned").GetInt32());
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
        var policyId = db.SlaPolicies.Single(p => p.EntityType == "assignment").Id;
        Assert.False(db.SlaTrackings.Any(t => t.SlaPolicyId == policyId && t.EntityId == id1 && t.ResolvedAt == null));
    }

    [Fact]
    public async Task Create_AsEvaluator_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "evaluator1");

        var response = await client.PostAsJsonAsync("/api/admin/assignments", new { ideaId = IdeaId, evaluatorId = EvaluatorId, dueAt = (DateTime?)null, notes = (string?)null });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    public void Dispose() => _connection.Dispose();
}
