using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Infrastructure.Auth;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InnovationToImpact.Api.Tests;

public class PostProgramEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private Guid _ideaId;

    public PostProgramEndpointTests(WebApplicationFactory<Program> factory)
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
        db.Users.Add(new User { Id = adminId, SamAccountName = "admin1", Email = "admin1@gac-demo.sa", FullNameAr = "a", FullNameEn = "a" });
        var submitterId = Guid.NewGuid();
        db.Users.Add(new User { Id = submitterId, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "s", FullNameEn = "s" });
        db.SaveChanges();
        db.Set<UserRole>().Add(new UserRole { UserId = adminId, RoleId = adminRoleId, IsPrimary = true });
        db.Set<UserRole>().Add(new UserRole { UserId = submitterId, RoleId = submitterRoleId, IsPrimary = true });
        var approved = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Approved);
        var theme = db.StrategicThemes.First();
        var activity = new Activity { Id = Guid.NewGuid(), NameAr = "n", NameEn = "Activity", Type = "event", Status = "open", CreatedById = submitterId };
        db.Activities.Add(activity);
        _ideaId = Guid.NewGuid();
        db.Ideas.Add(new Idea
        {
            Id = _ideaId, Code = "IDEA-0001", TitleAr = "ف", TitleEn = "Idea",
            ProblemStatementAr = "م", ProblemStatementEn = "P", ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ب", ExpectedBenefitsEn = "B",
            StrategicThemeId = theme.Id, ActivityId = activity.Id, SubmitterId = submitterId,
            IdeaStatusId = approved.Id, ParticipationType = "individual",
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task Advance_AsAdmin_ApprovedToInPilot_Ok()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");
        var res = await client.PostAsJsonAsync($"/api/admin/ideas/{_ideaId}/post-program-stage", new { stage = "in_pilot" });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("in_pilot", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Advance_SkippingStage_BadRequest()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");
        var res = await client.PostAsJsonAsync($"/api/admin/ideas/{_ideaId}/post-program-stage", new { stage = "in_scaling" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Advance_AsSubmitter_Forbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");
        var res = await client.PostAsJsonAsync($"/api/admin/ideas/{_ideaId}/post-program-stage", new { stage = "in_pilot" });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task ListPostProgram_AsAdmin_IncludesApprovedIdea()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");
        var res = await client.GetAsync("/api/admin/post-program/ideas");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;
        Assert.True(body.GetArrayLength() >= 1);
        Assert.Equal("IDEA-0001", body[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task IdeaDetail_IncludesSubmitterId()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");
        var res = await client.GetAsync($"/api/ideas/{_ideaId}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;
        Assert.True(body.TryGetProperty("submitterId", out var sid) && sid.ValueKind != JsonValueKind.Null);
    }

    public void Dispose() => _connection.Dispose();
}
