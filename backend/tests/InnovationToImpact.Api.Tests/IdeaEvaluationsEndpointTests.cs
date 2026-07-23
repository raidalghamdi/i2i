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

public class IdeaEvaluationsEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private Guid _ideaId;

    public IdeaEvaluationsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("submitter1", "Submitter One", "submitter1@gac-demo.sa", null, null, null),
            new AdIdentity("submitter2", "Submitter Two", "submitter2@gac-demo.sa", null, null, null),
            new AdIdentity("evaluator1", "Evaluator One", "evaluator1@gac-demo.sa", null, null, null),
            new AdIdentity("evaluator2", "Evaluator Two", "evaluator2@gac-demo.sa", null, null, null),
            new AdIdentity("evaluator3", "Evaluator Three", "evaluator3@gac-demo.sa", null, null, null),
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
        var submitterRoleId = db.Roles.Single(r => r.Code == "submitter").Id;
        var evaluatorRoleId = db.Roles.Single(r => r.Code == "evaluator").Id;

        var submitter1Id = Guid.NewGuid();
        var submitter2Id = Guid.NewGuid();
        var evaluator1Id = Guid.NewGuid();
        var evaluator2Id = Guid.NewGuid();
        var evaluator3Id = Guid.NewGuid();
        db.Users.Add(new User { Id = submitter1Id, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "s1", FullNameEn = "s1" });
        db.Users.Add(new User { Id = submitter2Id, SamAccountName = "submitter2", Email = "submitter2@gac-demo.sa", FullNameAr = "s2", FullNameEn = "s2" });
        db.Users.Add(new User { Id = evaluator1Id, SamAccountName = "evaluator1", Email = "evaluator1@gac-demo.sa", FullNameAr = "e1", FullNameEn = "e1" });
        db.Users.Add(new User { Id = evaluator2Id, SamAccountName = "evaluator2", Email = "evaluator2@gac-demo.sa", FullNameAr = "e2", FullNameEn = "e2" });
        db.Users.Add(new User { Id = evaluator3Id, SamAccountName = "evaluator3", Email = "evaluator3@gac-demo.sa", FullNameAr = "e3", FullNameEn = "e3" });
        db.SaveChanges();
        db.Set<UserRole>().Add(new UserRole { UserId = submitter1Id, RoleId = submitterRoleId, IsPrimary = true });
        db.Set<UserRole>().Add(new UserRole { UserId = submitter2Id, RoleId = submitterRoleId, IsPrimary = true });
        db.Set<UserRole>().Add(new UserRole { UserId = evaluator1Id, RoleId = evaluatorRoleId, IsPrimary = true });
        db.Set<UserRole>().Add(new UserRole { UserId = evaluator2Id, RoleId = evaluatorRoleId, IsPrimary = true });
        db.Set<UserRole>().Add(new UserRole { UserId = evaluator3Id, RoleId = evaluatorRoleId, IsPrimary = true });
        db.SaveChanges();

        var themeId = db.StrategicThemes.First().Id;
        var statusId = db.IdeaStatuses.Single(s => s.Code == "pass_awaiting_attachments").Id;
        _ideaId = Guid.NewGuid();
        db.Ideas.Add(new Idea
        {
            Id = _ideaId, Code = "IDEA-0001", TitleAr = "ت", TitleEn = "T", ProblemStatementAr = "م", ProblemStatementEn = "P",
            ProposedSolutionAr = "ح", ProposedSolutionEn = "S", ExpectedBenefitsAr = "ف", ExpectedBenefitsEn = "B",
            StrategicThemeId = themeId, IdeaStatusId = statusId, SubmitterId = submitter1Id,
        });
        db.SaveChanges();

        // Submitted evaluation from evaluator1 (earlier) and evaluator2 (later) — order by SubmittedAt.
        db.Evaluations.Add(new Evaluation
        {
            Id = Guid.NewGuid(), IdeaId = _ideaId, EvaluatorId = evaluator2Id, TotalScore = 8.0m,
            Comments = "Second submitted", SubmittedAt = new DateTime(2026, 7, 2, 0, 0, 0, DateTimeKind.Utc),
        });
        db.Evaluations.Add(new Evaluation
        {
            Id = Guid.NewGuid(), IdeaId = _ideaId, EvaluatorId = evaluator1Id, TotalScore = 6.0m,
            Comments = "First submitted", SubmittedAt = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        // Draft (not submitted) evaluation — must be excluded entirely.
        db.Evaluations.Add(new Evaluation
        {
            Id = Guid.NewGuid(), IdeaId = _ideaId, EvaluatorId = evaluator3Id, TotalScore = 0m,
            Comments = null, SubmittedAt = null,
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task Get_AsOwningSubmitter_ReturnsAnonymizedOrderedEvaluations()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.GetAsync($"/api/ideas/{_ideaId}/evaluations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var evaluations = body.GetProperty("evaluations").EnumerateArray().ToList();

        Assert.Equal(2, evaluations.Count);
        Assert.Equal("Reviewer 1", evaluations[0].GetProperty("reviewerLabel").GetString());
        Assert.Equal(6.0, evaluations[0].GetProperty("score").GetDouble());
        Assert.Equal("First submitted", evaluations[0].GetProperty("comment").GetString());
        Assert.Equal("Reviewer 2", evaluations[1].GetProperty("reviewerLabel").GetString());
        Assert.Equal(8.0, evaluations[1].GetProperty("score").GetDouble());

        Assert.Equal(7.0, body.GetProperty("averageScore").GetDouble());

        foreach (var e in evaluations)
        {
            Assert.False(e.TryGetProperty("evaluatorId", out _));
        }
    }

    [Fact]
    public async Task Get_AsNonOwningSubmitter_Returns404()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter2");

        var response = await client.GetAsync($"/api/ideas/{_ideaId}/evaluations");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_IdeaWithNoSubmittedEvaluations_ReturnsEmptyListAndNullAverage()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
            db.Evaluations.RemoveRange(db.Evaluations.Where(e => e.IdeaId == _ideaId && e.SubmittedAt != null));
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/ideas/{_ideaId}/evaluations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Empty(body.GetProperty("evaluations").EnumerateArray());
        Assert.True(body.GetProperty("averageScore").ValueKind == JsonValueKind.Null);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
