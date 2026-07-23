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
using Xunit;

namespace InnovationToImpact.Api.Tests;

public class MyIdeasEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;

    public MyIdeasEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("sub1", "Sub One", "sub1@gac-demo.sa", "IT", null, null),
            new AdIdentity("eval1", "Eval One", "eval1@gac-demo.sa", "IT", null, null),
            new AdIdentity("judge1", "Judge One", "judge1@gac-demo.sa", "IT", null, null),
        });
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<InnovationDbContext>>();
                services.AddDbContext<InnovationDbContext>(o => o.UseSqlite(_connection));
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
        var submitterRole = db.Roles.Single(r => r.Code == RoleCodes.Submitter);
        var evaluatorRole = db.Roles.Single(r => r.Code == RoleCodes.Evaluator);
        var judgeRole = db.Roles.Single(r => r.Code == RoleCodes.Judge);

        var sub = new User { Id = Guid.NewGuid(), SamAccountName = "sub1", Email = "sub1@gac-demo.sa", FullNameAr = "م", FullNameEn = "Sub One", Department = "IT" };
        var eval = new User { Id = Guid.NewGuid(), SamAccountName = "eval1", Email = "eval1@gac-demo.sa", FullNameAr = "ق", FullNameEn = "Eval One", Department = "IT" };
        var judge = new User { Id = Guid.NewGuid(), SamAccountName = "judge1", Email = "judge1@gac-demo.sa", FullNameAr = "ح", FullNameEn = "Judge One", Department = "IT" };
        db.Users.AddRange(sub, eval, judge);
        db.SaveChanges();
        db.Set<UserRole>().AddRange(
            new UserRole { UserId = sub.Id, RoleId = submitterRole.Id, IsPrimary = true },
            new UserRole { UserId = eval.Id, RoleId = evaluatorRole.Id, IsPrimary = true },
            new UserRole { UserId = judge.Id, RoleId = judgeRole.Id, IsPrimary = true });
        db.SaveChanges();

        var theme = db.StrategicThemes.First();
        var submittedStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Submitted);
        var evaluationStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Evaluation);
        var committeeStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Committee);
        var approvedStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Approved);
        var returnedStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Returned);

        Idea MkIdea(string code, IdeaStatus st) => new()
        {
            Id = Guid.NewGuid(), Code = code, TitleAr = "ع", TitleEn = code,
            ProblemStatementAr = "م", ProblemStatementEn = "P", ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ب", ExpectedBenefitsEn = "B",
            StrategicThemeId = theme.Id, SubmitterId = sub.Id, IdeaStatusId = st.Id, ParticipationType = "individual",
            CurrentStage = 2,
        };

        var ideaSubmitted = MkIdea("I-SUBMITTED", submittedStatus);
        var ideaEvaluation = MkIdea("I-EVAL", evaluationStatus);
        var ideaCommittee = MkIdea("I-COMMITTEE", committeeStatus);
        var ideaApproved = MkIdea("I-APPROVED", approvedStatus);
        var ideaReturned = MkIdea("I-RETURNED", returnedStatus);

        db.Ideas.AddRange(ideaSubmitted, ideaEvaluation, ideaCommittee, ideaApproved, ideaReturned);
        db.SaveChanges();

        // non-empty evaluation comment -> counts as feedback
        db.Evaluations.Add(new Evaluation
        {
            Id = Guid.NewGuid(), IdeaId = ideaEvaluation.Id, EvaluatorId = eval.Id,
            CriteriaScoresJson = "{}", TotalScore = 7m, Comments = "good", SubmittedAt = DateTime.UtcNow,
        });

        // non-empty committee decision comment -> counts as feedback
        db.CommitteeDecisions.Add(new CommitteeDecision
        {
            Id = Guid.NewGuid(), IdeaId = ideaCommittee.Id, CommitteeName = "Main",
            CommitteeDecisionTypeId = db.CommitteeDecisionTypes.First().Id, TotalScore = 8m, QuorumMet = true,
            Comments = "revise", DecidedAt = DateTime.UtcNow, DecidedById = judge.Id,
        });

        // whitespace-only comment -> must NOT count as feedback
        db.Evaluations.Add(new Evaluation
        {
            Id = Guid.NewGuid(), IdeaId = ideaApproved.Id, EvaluatorId = eval.Id,
            CriteriaScoresJson = "{}", TotalScore = 9m, Comments = "   ", SubmittedAt = DateTime.UtcNow,
        });

        db.SaveChanges();
    }

    private HttpClient ClientFor(string sam)
    {
        var c = _factory.CreateClient();
        c.DefaultRequestHeaders.Add("X-Dev-User", sam);
        return c;
    }

    [Fact]
    public async Task MyIdeas_ReturnsCurrentStageCreatedAtAndFeedbackCount()
    {
        var res = await ClientFor("sub1").GetAsync("/api/ideas/mine");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.EnumerateArray().ToList();
        Assert.Equal(5, items.Count);

        var byCode = items.ToDictionary(i => i.GetProperty("code").GetString()!);

        foreach (var item in items)
        {
            Assert.True(item.GetProperty("currentStage").GetInt32() >= 0);
            Assert.True(item.TryGetProperty("createdAt", out _));
        }

        Assert.Equal(1, byCode["I-EVAL"].GetProperty("feedbackCount").GetInt32());
        Assert.Equal(1, byCode["I-COMMITTEE"].GetProperty("feedbackCount").GetInt32());
        Assert.Equal(0, byCode["I-SUBMITTED"].GetProperty("feedbackCount").GetInt32());
        // whitespace-only comment must NOT count
        Assert.Equal(0, byCode["I-APPROVED"].GetProperty("feedbackCount").GetInt32());
        Assert.Equal(0, byCode["I-RETURNED"].GetProperty("feedbackCount").GetInt32());
    }

    [Fact]
    public async Task MyIdeas_StatusGroupApproved_ReturnsOnlyApprovedGroupIdeas()
    {
        var res = await ClientFor("sub1").GetAsync("/api/ideas/mine?statusGroup=approved");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.EnumerateArray().ToList();
        Assert.Single(items);
        Assert.Equal("I-APPROVED", items[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task MyIdeas_StatusGroupInReview_ReturnsOnlyInReviewGroupIdeas()
    {
        var res = await ClientFor("sub1").GetAsync("/api/ideas/mine?statusGroup=in_review");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        var codes = body.EnumerateArray().Select(i => i.GetProperty("code").GetString()).ToHashSet();
        Assert.Equal(new HashSet<string?> { "I-SUBMITTED", "I-EVAL", "I-COMMITTEE" }, codes);
    }

    [Fact]
    public async Task MyIdeas_StatusGroupReturned_ReturnsOnlyReturnedGroupIdeas()
    {
        var res = await ClientFor("sub1").GetAsync("/api/ideas/mine?statusGroup=returned");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.EnumerateArray().ToList();
        Assert.Single(items);
        Assert.Equal("I-RETURNED", items[0].GetProperty("code").GetString());
    }

    public void Dispose() => _connection.Dispose();
}
