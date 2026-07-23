using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using InnovationToImpact.Domain.Approvals;
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

public class ApprovalEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string CommitteePublishChain = "committee-publish";

    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"approval-endpoint-test-{Guid.NewGuid():N}");
    private Guid _entityId;
    private Guid _secondEntityId;

    public ApprovalEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var lookup = new FakeAdIdentityLookupService(
            seedIdentities: new[]
            {
                new AdIdentity("evaluator1", "Evaluator One", "evaluator1@gac-demo.sa", null, null, null),
                new AdIdentity("judge1", "Judge One", "judge1@gac-demo.sa", null, null, null),
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

                var evaluatorRoleId = db.Roles.Single(r => r.Code == RoleCodes.Evaluator).Id;
                var evaluatorId = Guid.NewGuid();
                db.Users.Add(new User { Id = evaluatorId, SamAccountName = "evaluator1", Email = "evaluator1@gac-demo.sa", FullNameAr = "evaluator1", FullNameEn = "evaluator1" });
                var judgeRoleId = db.Roles.Single(r => r.Code == RoleCodes.Judge).Id;
                var judgeId = Guid.NewGuid();
                db.Users.Add(new User { Id = judgeId, SamAccountName = "judge1", Email = "judge1@gac-demo.sa", FullNameAr = "judge1", FullNameEn = "judge1" });
                var submitterRoleId = db.Roles.Single(r => r.Code == RoleCodes.Submitter).Id;
                var submitterId = Guid.NewGuid();
                db.Users.Add(new User { Id = submitterId, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "submitter1", FullNameEn = "submitter1" });
                db.SaveChanges();
                db.Set<UserRole>().AddRange(
                    new UserRole { UserId = evaluatorId, RoleId = evaluatorRoleId, IsPrimary = true },
                    new UserRole { UserId = judgeId, RoleId = judgeRoleId, IsPrimary = true },
                    new UserRole { UserId = submitterId, RoleId = submitterRoleId, IsPrimary = true });
                db.SaveChanges();

                var approvalService = scope.ServiceProvider.GetRequiredService<IApprovalService>();
                _entityId = Guid.NewGuid();
                _secondEntityId = Guid.NewGuid();
                approvalService.OpenInstanceAsync(CommitteePublishChain, "committee_decision", _entityId).GetAwaiter().GetResult();
                approvalService.OpenInstanceAsync(CommitteePublishChain, "committee_decision", _secondEntityId).GetAwaiter().GetResult();
            });
        });
    }

    [Fact]
    public async Task Get_AsEvaluator_ReturnsPendingStep1Card()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "evaluator1");

        var response = await client.GetAsync("/api/approvals");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(2, items.Count);
        Assert.Contains(items, i => i.GetProperty("entityId").GetGuid() == _entityId && i.GetProperty("stepOrder").GetInt32() == 1);
    }

    [Fact]
    public async Task Get_AsSubmitter_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.GetAsync("/api/approvals");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Decide_ApproveAsEvaluator_AdvancesInstanceAndStaysPending()
    {
        var evaluatorClient = _factory.CreateClient();
        evaluatorClient.DefaultRequestHeaders.Add("X-Dev-User", "evaluator1");

        var listResponse = await evaluatorClient.GetAsync("/api/approvals");
        var listBody = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var card = listBody.GetProperty("items").EnumerateArray().First(i => i.GetProperty("entityId").GetGuid() == _entityId);
        var instanceId = card.GetProperty("instanceId").GetGuid();
        var stepId = card.GetProperty("stepId").GetGuid();

        var decideResponse = await evaluatorClient.PostAsJsonAsync("/api/approvals/decide", new { instanceId, stepId, decision = "approve", comment = "looks good" });

        Assert.Equal(HttpStatusCode.OK, decideResponse.StatusCode);
        var decideBody = await decideResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("pending", decideBody.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Decide_InvalidDecisionValue_ReturnsBadRequest()
    {
        var evaluatorClient = _factory.CreateClient();
        evaluatorClient.DefaultRequestHeaders.Add("X-Dev-User", "evaluator1");

        var listResponse = await evaluatorClient.GetAsync("/api/approvals");
        var listBody = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var card = listBody.GetProperty("items").EnumerateArray().First(i => i.GetProperty("entityId").GetGuid() == _entityId);
        var instanceId = card.GetProperty("instanceId").GetGuid();
        var stepId = card.GetProperty("stepId").GetGuid();

        var decideResponse = await evaluatorClient.PostAsJsonAsync("/api/approvals/decide", new { instanceId, stepId, decision = "request_changes", comment = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, decideResponse.StatusCode);
    }

    [Fact]
    public async Task BulkDecide_TwoValidTargets_ReturnsSucceededCounts()
    {
        var evaluatorClient = _factory.CreateClient();
        evaluatorClient.DefaultRequestHeaders.Add("X-Dev-User", "evaluator1");

        var listResponse = await evaluatorClient.GetAsync("/api/approvals");
        var listBody = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var targets = listBody.GetProperty("items").EnumerateArray()
            .Select(i => new { instanceId = i.GetProperty("instanceId").GetGuid(), stepId = i.GetProperty("stepId").GetGuid() })
            .ToList();
        Assert.Equal(2, targets.Count);

        var bulkResponse = await evaluatorClient.PostAsJsonAsync("/api/approvals/bulk-decide", new { targets, decision = "approve", comment = "batch ok" });

        Assert.Equal(HttpStatusCode.OK, bulkResponse.StatusCode);
        var bulkBody = await bulkResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, bulkBody.GetProperty("succeeded").GetInt32());
        Assert.Empty(bulkBody.GetProperty("failed").EnumerateArray());
    }

    [Fact]
    public async Task BulkDecide_InvalidDecisionValue_ReturnsBadRequest()
    {
        var evaluatorClient = _factory.CreateClient();
        evaluatorClient.DefaultRequestHeaders.Add("X-Dev-User", "evaluator1");

        var listResponse = await evaluatorClient.GetAsync("/api/approvals");
        var listBody = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var targets = listBody.GetProperty("items").EnumerateArray()
            .Select(i => new { instanceId = i.GetProperty("instanceId").GetGuid(), stepId = i.GetProperty("stepId").GetGuid() })
            .ToList();

        var bulkResponse = await evaluatorClient.PostAsJsonAsync("/api/approvals/bulk-decide", new { targets, decision = "maybe", comment = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, bulkResponse.StatusCode);
    }

    public void Dispose()
    {
        _connection.Dispose();
        if (Directory.Exists(_rootPath)) Directory.Delete(_rootPath, recursive: true);
    }
}
