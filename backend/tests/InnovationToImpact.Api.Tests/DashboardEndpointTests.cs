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

public class DashboardEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;

    public DashboardEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("admin1", "Admin One", "admin1@gac-demo.sa", "IT", null, null),
            new AdIdentity("judge1", "Judge One", "judge1@gac-demo.sa", "IT", null, null),
            new AdIdentity("sub1", "Sub One", "sub1@gac-demo.sa", "IT", null, null),
            new AdIdentity("sup1", "Sup One", "sup1@gac-demo.sa", "IT", null, null),
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
        var adminRole = db.Roles.Single(r => r.Code == "admin");
        var judgeRole = db.Roles.Single(r => r.Code == "judge");
        var subRole = db.Roles.Single(r => r.Code == "submitter");
        var supervisorRole = db.Roles.Single(r => r.Code == RoleCodes.Supervisor);

        var admin = new User { Id = Guid.NewGuid(), SamAccountName = "admin1", Email = "admin1@gac-demo.sa", FullNameAr = "أ", FullNameEn = "Admin One", Department = "IT" };
        var judge = new User { Id = Guid.NewGuid(), SamAccountName = "judge1", Email = "judge1@gac-demo.sa", FullNameAr = "ج", FullNameEn = "Judge One", Department = "IT" };
        var sub = new User { Id = Guid.NewGuid(), SamAccountName = "sub1", Email = "sub1@gac-demo.sa", FullNameAr = "م", FullNameEn = "Sub One", Department = "IT" };
        var sup = new User { Id = Guid.NewGuid(), SamAccountName = "sup1", Email = "sup1@gac-demo.sa", FullNameAr = "ش", FullNameEn = "Sup One", Department = "IT" };
        db.Users.AddRange(admin, judge, sub, sup);
        db.SaveChanges();
        db.Set<UserRole>().AddRange(
            new UserRole { UserId = admin.Id, RoleId = adminRole.Id, IsPrimary = true },
            new UserRole { UserId = judge.Id, RoleId = judgeRole.Id, IsPrimary = true },
            new UserRole { UserId = sub.Id, RoleId = subRole.Id, IsPrimary = true },
            new UserRole { UserId = sup.Id, RoleId = supervisorRole.Id, IsPrimary = true });
        db.SaveChanges();

        var theme = db.StrategicThemes.First();
        var committeeStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Committee);
        var approvedStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Approved);
        var rejectedStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Rejected);
        var evaluationFailedStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.EvaluationFailed);
        var notSelectedStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.NotSelected);
        var draftStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Draft);

        Idea MkIdea(string code, IdeaStatus st) => new()
        {
            Id = Guid.NewGuid(), Code = code, TitleAr = "ع", TitleEn = code,
            ProblemStatementAr = "م", ProblemStatementEn = "P", ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ب", ExpectedBenefitsEn = "B",
            StrategicThemeId = theme.Id, SubmitterId = sub.Id, IdeaStatusId = st.Id, ParticipationType = "individual",
        };
        db.Ideas.AddRange(
            MkIdea("I-COMMITTEE", committeeStatus),
            MkIdea("I-APPROVED", approvedStatus),
            MkIdea("I-REJECTED", rejectedStatus),
            MkIdea("I-EVAL-FAILED", evaluationFailedStatus),
            MkIdea("I-NOT-SELECTED", notSelectedStatus),
            MkIdea("I-DRAFT", draftStatus));
        db.SaveChanges();

        // one committee decision this week
        db.CommitteeDecisions.Add(new CommitteeDecision
        {
            Id = Guid.NewGuid(), IdeaId = db.Ideas.First(i => i.Code == "I-APPROVED").Id, CommitteeName = "Main",
            CommitteeDecisionTypeId = db.CommitteeDecisionTypes.First().Id, TotalScore = 8m, QuorumMet = true,
            DecidedAt = DateTime.UtcNow, DecidedById = judge.Id,
        });
        // one committee decision older than the 7-day window — must NOT count toward decisionsThisWeek
        db.CommitteeDecisions.Add(new CommitteeDecision
        {
            Id = Guid.NewGuid(), IdeaId = db.Ideas.First(i => i.Code == "I-REJECTED").Id, CommitteeName = "Main",
            CommitteeDecisionTypeId = db.CommitteeDecisionTypes.First().Id, TotalScore = 3m, QuorumMet = true,
            DecidedAt = DateTime.UtcNow.AddDays(-10), DecidedById = judge.Id,
        });
        db.SaveChanges();

        // one pending assignment — evaluator=judge, assigned by admin, on the committee idea
        var pendingStatus = db.Set<AssignmentStatus>().Single(s => s.Code == "pending");
        db.Assignments.Add(new Assignment
        {
            Id = Guid.NewGuid(),
            IdeaId = db.Ideas.First(i => i.Code == "I-COMMITTEE").Id,
            EvaluatorId = judge.Id,
            AssignedById = admin.Id,
            AssignedAt = DateTime.UtcNow,
            AssignmentStatusId = pendingStatus.Id,
        });
        db.SaveChanges();

        // one open escalation on the committee idea, owned by the supervisor
        db.Escalations.Add(new Escalation
        {
            Id = Guid.NewGuid(),
            EntityType = "idea",
            EntityId = db.Ideas.First(i => i.Code == "I-COMMITTEE").Id,
            EscalationTierId = db.EscalationTiers.First().Id,
            ReasonAr = "س",
            ReasonEn = "reason",
            EscalationStatusId = db.EscalationStatuses.Single(s => s.Code == "open").Id,
            OwnerId = sup.Id,
            OpenedAt = DateTime.UtcNow,
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
    public async Task AdminDashboard_AsAdmin_ReturnsCounts()
    {
        var res = await ClientFor("admin1").GetAsync("/api/dashboard/admin");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        // 4 seeded test users (admin, judge, sub, sup) + 1 seeded "system" account (SeedStrategicThemesAndSystemUser migration) = 5
        Assert.Equal(5, body.GetProperty("totalUsers").GetInt32());
        // active = not in {rejected, evaluation_failed, not_selected}: committee + approved + draft = 3
        // (I-REJECTED, I-EVAL-FAILED, I-NOT-SELECTED are all terminal and excluded; I-DRAFT is not terminal so it counts)
        Assert.Equal(3, body.GetProperty("activeIdeas").GetInt32());
        Assert.Equal(1, body.GetProperty("pendingEvaluations").GetInt32());
        Assert.Equal("Healthy", body.GetProperty("health").GetString());
    }

    [Fact]
    public async Task AdminDashboard_AsSubmitter_Forbidden()
    {
        var res = await ClientFor("sub1").GetAsync("/api/dashboard/admin");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task CommitteeDashboard_AsJudge_ReturnsCounts()
    {
        var res = await ClientFor("judge1").GetAsync("/api/dashboard/committee");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("awaitingDecision").GetInt32()); // one idea in 'committee'
        Assert.Equal(1, body.GetProperty("decisionsThisWeek").GetInt32());
    }

    [Fact]
    public async Task CommitteeDashboard_AsSubmitter_Forbidden()
    {
        var res = await ClientFor("sub1").GetAsync("/api/dashboard/committee");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task SupervisorDashboard_AsSupervisor_ReturnsSectorAndBuckets()
    {
        var res = await ClientFor("sup1").GetAsync("/api/dashboard/supervisor");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("teamMembers").GetInt32() >= 4);           // users in dept "IT": admin, judge, sub, sup
        // all 6 seeded ideas (incl. I-DRAFT) have submitter dept "IT" — sectorIdeas is NOT draft-filtered
        Assert.Equal(6, body.GetProperty("sectorIdeas").GetInt32());
        Assert.Equal(1, body.GetProperty("escalationsAwaitingMe").GetInt32());  // one open escalation owned by sup1
        var screening = body.GetProperty("screening");
        // total excludes drafts: 5 non-draft ideas (committee/approved/rejected/evaluation_failed/not_selected);
        // I-DRAFT must NOT change this count.
        var total = screening.GetProperty("total").GetInt32();
        var underReview = screening.GetProperty("underReview").GetInt32();
        var approved = screening.GetProperty("approved").GetInt32();
        var returned = screening.GetProperty("returned").GetInt32();
        var rejected = screening.GetProperty("rejected").GetInt32();
        Assert.Equal(5, total);
        Assert.Equal(1, underReview);       // I-COMMITTEE
        Assert.Equal(1, approved);          // I-APPROVED
        Assert.Equal(0, returned);
        Assert.Equal(3, rejected);          // I-REJECTED, I-EVAL-FAILED, I-NOT-SELECTED
        // reconciliation: the buckets must sum exactly to the total (draft is excluded from both sides)
        Assert.Equal(total, underReview + approved + returned + rejected);
    }

    [Fact]
    public async Task SupervisorDashboard_AsSubmitter_Forbidden()
    {
        var res = await ClientFor("sub1").GetAsync("/api/dashboard/supervisor");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    public void Dispose() => _connection.Dispose();
}
