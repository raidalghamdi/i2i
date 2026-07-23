using System.Net;
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

public class IdeaJourneyEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private Guid _ideaId;

    public IdeaJourneyEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("submitter1", "Submitter One", "submitter1@gac-demo.sa", null, null, null),
            new AdIdentity("outsider1", "Outsider One", "outsider1@gac-demo.sa", null, null, null),
            new AdIdentity("submitter2", "Submitter Two", "submitter2@gac-demo.sa", null, null, null),
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

                using var scope = services.BuildServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
                db.Database.EnsureCreated();
                Seed(db);
            });
        });
    }

    private void Seed(InnovationDbContext db)
    {
        var submitterId = Guid.NewGuid();
        db.Users.Add(new User { Id = submitterId, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "س", FullNameEn = "Submitter One" });
        var submitterRole = db.Roles.Single(r => r.Code == "submitter");
        db.Set<UserRole>().Add(new UserRole { UserId = submitterId, RoleId = submitterRole.Id, IsPrimary = true });

        var committeeStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Committee);
        var theme = db.StrategicThemes.First();
        var activity = new Activity { Id = Guid.NewGuid(), NameAr = "ن", NameEn = "Activity", Type = "event", Status = "open", CreatedById = submitterId };
        db.Activities.Add(activity);

        _ideaId = Guid.NewGuid();
        db.Ideas.Add(new Idea
        {
            Id = _ideaId, Code = "IDEA-0001", TitleAr = "ف", TitleEn = "Idea",
            ProblemStatementAr = "م", ProblemStatementEn = "P", ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ب", ExpectedBenefitsEn = "B",
            StrategicThemeId = theme.Id, ActivityId = activity.Id,
            SubmitterId = submitterId, IdeaStatusId = committeeStatus.Id, CurrentStage = 1,
            ParticipationType = "individual",
        });
        var evaluatorId = Guid.NewGuid();
        db.Users.Add(new User { Id = evaluatorId, SamAccountName = "eval1", Email = "eval1@gac-demo.sa", FullNameAr = "ق", FullNameEn = "Eval" });
        db.Evaluations.Add(new Evaluation { Id = Guid.NewGuid(), IdeaId = _ideaId, EvaluatorId = evaluatorId, TotalScore = 8m, SubmittedAt = DateTime.UtcNow });

        var submitter2Id = Guid.NewGuid();
        db.Users.Add(new User { Id = submitter2Id, SamAccountName = "submitter2", Email = "submitter2@gac-demo.sa", FullNameAr = "س٢", FullNameEn = "Submitter Two" });
        db.Set<UserRole>().Add(new UserRole { UserId = submitter2Id, RoleId = submitterRole.Id, IsPrimary = true });

        var evaluator1Id = Guid.NewGuid();
        db.Users.Add(new User { Id = evaluator1Id, SamAccountName = "evaluator1", Email = "evaluator1@gac-demo.sa", FullNameAr = "ق١", FullNameEn = "Evaluator One" });
        var evaluatorRole = db.Roles.Single(r => r.Code == "evaluator");
        db.Set<UserRole>().Add(new UserRole { UserId = evaluator1Id, RoleId = evaluatorRole.Id, IsPrimary = true });

        db.SaveChanges();
    }

    [Fact]
    public async Task Owner_GetsEightStagesWithCommitteeCurrent()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.GetAsync($"/api/ideas/{_ideaId}/journey");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var stages = root.GetProperty("stages");
        Assert.Equal(8, stages.GetArrayLength());
        Assert.Equal("current", stages[3].GetProperty("state").GetString());
        Assert.Equal("Committee Review", stages[3].GetProperty("label").GetProperty("en").GetString());
    }

    [Fact]
    public async Task NonOwnerWithoutRole_IsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "outsider1");

        var response = await client.GetAsync($"/api/ideas/{_ideaId}/journey");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task NonOwnerWithRole_IsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter2");

        var response = await client.GetAsync($"/api/ideas/{_ideaId}/journey");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ElevatedReviewer_NonOwner_GetsEightStages()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "evaluator1");

        var response = await client.GetAsync($"/api/ideas/{_ideaId}/journey");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var stages = root.GetProperty("stages");
        Assert.Equal(8, stages.GetArrayLength());
    }

    public void Dispose() => _connection.Dispose();
}
