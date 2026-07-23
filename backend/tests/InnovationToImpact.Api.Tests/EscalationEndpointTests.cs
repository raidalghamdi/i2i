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

public class EscalationEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"escalation-endpoint-test-{Guid.NewGuid():N}");
    private Guid _escalationId;

    public EscalationEndpointTests(WebApplicationFactory<Program> factory)
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

                var openStatusId = db.EscalationStatuses.Single(s => s.Code == "open").Id;
                var tier1Id = db.EscalationTiers.Single(t => t.Code == "manager").Id;
                _escalationId = Guid.NewGuid();
                db.Escalations.Add(new Escalation
                {
                    Id = _escalationId,
                    EntityType = "evaluation",
                    EntityId = Guid.NewGuid(),
                    EscalationTierId = tier1Id,
                    EscalationStatusId = openStatusId,
                    ReasonAr = "أ",
                    ReasonEn = "test breach",
                });
                db.SaveChanges();
            });
        });
    }

    [Fact]
    public async Task ListGetAcknowledgeBumpResolve_FullWorkflow_Succeeds()
    {
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var listResponse = await adminClient.GetAsync("/api/admin/escalations");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listBody = await listResponse.Content.ReadAsStringAsync();
        Assert.Contains("test breach", listBody);

        var getResponse = await adminClient.GetAsync($"/api/admin/escalations/{_escalationId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var ackResponse = await adminClient.PostAsJsonAsync($"/api/admin/escalations/{_escalationId}/acknowledge", new { notes = "looking into it" });
        Assert.Equal(HttpStatusCode.OK, ackResponse.StatusCode);
        var ackBody = await ackResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("acknowledged", ackBody.GetProperty("statusCode").GetString());

        var bumpResponse = await adminClient.PostAsJsonAsync($"/api/admin/escalations/{_escalationId}/bump", new { notes = (string?)null });
        Assert.Equal(HttpStatusCode.OK, bumpResponse.StatusCode);
        var bumpBody = await bumpResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("director", bumpBody.GetProperty("tierCode").GetString());
        Assert.Equal("open", bumpBody.GetProperty("statusCode").GetString());

        var resolveResponse = await adminClient.PostAsJsonAsync($"/api/admin/escalations/{_escalationId}/resolve", new { resolutionAr = "تم", resolutionEn = "fixed" });
        Assert.Equal(HttpStatusCode.OK, resolveResponse.StatusCode);
        var resolveBody = await resolveResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("resolved", resolveBody.GetProperty("statusCode").GetString());
    }

    [Fact]
    public async Task Acknowledge_AsNonAdmin_ReturnsForbidden()
    {
        var submitterClient = _factory.CreateClient();
        submitterClient.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await submitterClient.PostAsJsonAsync($"/api/admin/escalations/{_escalationId}/acknowledge", new { notes = (string?)null });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Resolve_MissingResolutionText_ReturnsBadRequest()
    {
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await adminClient.PostAsJsonAsync($"/api/admin/escalations/{_escalationId}/resolve", new { resolutionAr = "", resolutionEn = "fixed" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_AsSupervisor_Succeeds()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.GetAsync("/api/admin/escalations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_AsSubmitter_Forbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.GetAsync("/api/admin/escalations");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    public void Dispose()
    {
        _connection.Dispose();
        if (Directory.Exists(_rootPath)) Directory.Delete(_rootPath, recursive: true);
    }
}
