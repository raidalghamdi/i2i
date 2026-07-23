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

public class SlaScanEscalationEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"sla-scan-escalation-test-{Guid.NewGuid():N}");

    public SlaScanEscalationEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var lookup = new FakeAdIdentityLookupService(
            seedIdentities: new[] { new AdIdentity("admin1", "Admin One", "admin1@gac-demo.sa", null, null, null) });

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
                db.SaveChanges();
                db.Set<UserRole>().Add(new UserRole { UserId = adminId, RoleId = adminRoleId, IsPrimary = true });
                db.SaveChanges();

                var policyId = db.SlaPolicies.Single(p => p.EntityType == "evaluation").Id;
                var breachedEntityId = Guid.NewGuid();
                db.SlaTrackings.Add(new SlaTracking
                {
                    Id = Guid.NewGuid(),
                    SlaPolicyId = policyId,
                    EntityId = breachedEntityId,
                    OpenedAt = DateTime.UtcNow.AddDays(-10),
                    TargetAt = DateTime.UtcNow.AddHours(-1),
                });
                db.SaveChanges();
            });
        });
    }

    [Fact]
    public async Task Scan_NewlyBreachedRow_OpensEscalationAndReportsCount()
    {
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await adminClient.PostAsync("/api/admin/sla/scan", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("newlyBreached").GetInt32());
        Assert.Equal(1, body.GetProperty("escalationsOpened").GetInt32());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
        var escalation = db.Escalations.Include(e => e.EscalationTier).Single(e => e.EntityType == "evaluation");
        Assert.Equal("manager", escalation.EscalationTier.Code);
        Assert.NotEmpty(escalation.ReasonEn);
    }

    [Fact]
    public async Task Scan_RunTwiceAfterSameBreach_DoesNotDuplicateEscalation()
    {
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        await adminClient.PostAsync("/api/admin/sla/scan", null);
        var secondResponse = await adminClient.PostAsync("/api/admin/sla/scan", null);
        var secondBody = await secondResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, secondBody.GetProperty("newlyBreached").GetInt32());
        Assert.Equal(0, secondBody.GetProperty("escalationsOpened").GetInt32());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
        Assert.Single(db.Escalations.Where(e => e.EntityType == "evaluation").ToList());
    }

    [Fact]
    public async Task Scan_TwoTrackingRowsBreachSameEntityInOneCall_OpensOnlyOneEscalation()
    {
        var sharedEntityId = Guid.NewGuid();

        using (var seedScope = _factory.Services.CreateScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<InnovationDbContext>();
            var policyId = seedDb.SlaPolicies.Single(p => p.EntityType == "evaluation").Id;

            seedDb.SlaTrackings.AddRange(
                new SlaTracking
                {
                    Id = Guid.NewGuid(),
                    SlaPolicyId = policyId,
                    EntityId = sharedEntityId,
                    OpenedAt = DateTime.UtcNow.AddDays(-10),
                    TargetAt = DateTime.UtcNow.AddHours(-1),
                },
                new SlaTracking
                {
                    Id = Guid.NewGuid(),
                    SlaPolicyId = policyId,
                    EntityId = sharedEntityId,
                    OpenedAt = DateTime.UtcNow.AddDays(-10),
                    TargetAt = DateTime.UtcNow.AddHours(-2),
                });
            seedDb.SaveChanges();
        }

        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await adminClient.PostAsync("/api/admin/sla/scan", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Both new rows plus the constructor-seeded row breach in this single call.
        Assert.Equal(3, body.GetProperty("newlyBreached").GetInt32());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
        var escalationsForSharedEntity = db.Escalations
            .Where(e => e.EntityType == "evaluation" && e.EntityId == sharedEntityId)
            .ToList();
        Assert.Single(escalationsForSharedEntity);
    }

    public void Dispose()
    {
        _connection.Dispose();
        if (Directory.Exists(_rootPath)) Directory.Delete(_rootPath, recursive: true);
    }
}
