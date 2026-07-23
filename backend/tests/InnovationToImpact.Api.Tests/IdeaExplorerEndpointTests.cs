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

public class IdeaExplorerEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;

    // ids populated by Seed(), used by the test methods below
    private Guid _themeOneId;
    private Guid _themeTwoId;
    private Guid _themeThreeId;

    public IdeaExplorerEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("admin1", "Admin One", "admin1@gac-demo.sa", "IT", null, null),
            new AdIdentity("judge1", "Judge One", "judge1@gac-demo.sa", "IT", null, null),
            new AdIdentity("sub1", "Sub One", "sub1@gac-demo.sa", "IT", null, null),
            new AdIdentity("sub2", "Sub Two", "sub2@gac-demo.sa", "IT", null, null),
            new AdIdentity("eval1", "Eval One", "eval1@gac-demo.sa", "IT", null, null),
            new AdIdentity("sub3", "Sub Three", "", "IT", null, null),
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
        var adminRole = db.Roles.Single(r => r.Code == RoleCodes.Admin);
        var judgeRole = db.Roles.Single(r => r.Code == RoleCodes.Judge);
        var subRole = db.Roles.Single(r => r.Code == RoleCodes.Submitter);
        var evaluatorRole = db.Roles.Single(r => r.Code == RoleCodes.Evaluator);

        var admin = new User { Id = Guid.NewGuid(), SamAccountName = "admin1", Email = "admin1@gac-demo.sa", FullNameAr = "أ", FullNameEn = "Admin One", Department = "IT" };
        var judge = new User { Id = Guid.NewGuid(), SamAccountName = "judge1", Email = "judge1@gac-demo.sa", FullNameAr = "ج", FullNameEn = "Judge One", Department = "IT" };
        var sub1 = new User { Id = Guid.NewGuid(), SamAccountName = "sub1", Email = "sub1@gac-demo.sa", FullNameAr = "م1", FullNameEn = "Sub One", Department = "IT" };
        var sub2 = new User { Id = Guid.NewGuid(), SamAccountName = "sub2", Email = "sub2@gac-demo.sa", FullNameAr = "م2", FullNameEn = "Sub Two", Department = "IT" };
        var eval1 = new User { Id = Guid.NewGuid(), SamAccountName = "eval1", Email = "eval1@gac-demo.sa", FullNameAr = "ق", FullNameEn = "Eval One", Department = "IT" };
        db.Users.AddRange(admin, judge, sub1, sub2, eval1);
        db.SaveChanges();

        db.Set<UserRole>().AddRange(
            new UserRole { UserId = admin.Id, RoleId = adminRole.Id, IsPrimary = true },
            new UserRole { UserId = judge.Id, RoleId = judgeRole.Id, IsPrimary = true },
            new UserRole { UserId = sub1.Id, RoleId = subRole.Id, IsPrimary = true },
            new UserRole { UserId = sub2.Id, RoleId = subRole.Id, IsPrimary = true },
            new UserRole { UserId = eval1.Id, RoleId = evaluatorRole.Id, IsPrimary = true });
        db.SaveChanges();

        var themes = db.StrategicThemes.OrderBy(t => t.Priority).ToList();
        _themeOneId = themes[0].Id;
        _themeTwoId = themes[1].Id;
        _themeThreeId = themes[2].Id;

        var draftStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Draft);
        var submittedStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Submitted);
        var approvedStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Approved);
        var committeeStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Committee);
        var inPilotStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.InPilot);
        var rejectedStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Rejected);

        Idea MkIdea(string code, IdeaStatus st, Guid themeId, Guid submitterId, int stage) => new()
        {
            Id = Guid.NewGuid(), Code = code, TitleAr = "ع", TitleEn = code,
            ProblemStatementAr = "م", ProblemStatementEn = "P", ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ب", ExpectedBenefitsEn = "B",
            StrategicThemeId = themeId, SubmitterId = submitterId, IdeaStatusId = st.Id,
            ParticipationType = "individual", CurrentStage = stage,
        };

        // sub1 owns A (draft, theme1, stage 0) and B (submitted, theme2, stage 1)
        var ideaA = MkIdea("IX-DRAFT-A", draftStatus, _themeOneId, sub1.Id, 0);
        var ideaB = MkIdea("IX-SUB-B", submittedStatus, _themeTwoId, sub1.Id, 1);
        // sub2 owns C (approved, theme1, stage 2) with sub1 as an email-team member -> sub1 must see it too
        var ideaC = MkIdea("IX-APPR-C", approvedStatus, _themeOneId, sub2.Id, 2);
        // sub2 owns D (committee, theme3, stage 2) -- unrelated to sub1, assigned to eval1
        var ideaD = MkIdea("IX-COMM-D", committeeStatus, _themeThreeId, sub2.Id, 2);
        // sub2 owns E (in_pilot, theme1, stage 3) -- finalist status, unrelated to sub1
        var ideaE = MkIdea("IX-PILOT-E", inPilotStatus, _themeOneId, sub2.Id, 3);
        // sub2 owns F (rejected, theme2, stage 1) -- unrelated to sub1, not a finalist status
        var ideaF = MkIdea("IX-REJ-F", rejectedStatus, _themeTwoId, sub2.Id, 1);

        db.Ideas.AddRange(ideaA, ideaB, ideaC, ideaD, ideaE, ideaF);
        db.SaveChanges();

        db.IdeaTeamMembers.Add(new IdeaTeamMember { Id = Guid.NewGuid(), IdeaId = ideaC.Id, Name = "Sub One", Email = "sub1@gac-demo.sa", SortOrder = 0 });
        db.SaveChanges();

        var pendingStatus = db.Set<AssignmentStatus>().Single(s => s.Code == "pending");
        db.Assignments.Add(new Assignment
        {
            Id = Guid.NewGuid(),
            IdeaId = ideaD.Id,
            EvaluatorId = eval1.Id,
            AssignedById = admin.Id,
            AssignedAt = DateTime.UtcNow,
            AssignmentStatusId = pendingStatus.Id,
        });
        db.SaveChanges();
    }

    private HttpClient ClientFor(string sam)
    {
        var c = _factory.CreateClient();
        c.DefaultRequestHeaders.Add("X-Dev-User", sam);
        return c;
    }

    private static HashSet<string> Codes(JsonElement body) =>
        body.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("code").GetString()!).ToHashSet();

    [Fact]
    public async Task Ideas_AsAdmin_SeesAllIdeas()
    {
        var res = await ClientFor("admin1").GetAsync("/api/ideas?pageSize=100");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(6, body.GetProperty("total").GetInt32());
        var codes = Codes(body);
        Assert.Equal(6, codes.Count);
    }

    [Fact]
    public async Task Ideas_AsSubmitter_SeesOwnIdeasAndEmailTeamIdea_NotOthers()
    {
        var res = await ClientFor("sub1").GetAsync("/api/ideas?pageSize=100");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, body.GetProperty("total").GetInt32());
        var codes = Codes(body);
        Assert.Contains("IX-DRAFT-A", codes);
        Assert.Contains("IX-SUB-B", codes);
        Assert.Contains("IX-APPR-C", codes); // email-team membership on sub2's idea
        Assert.DoesNotContain("IX-COMM-D", codes);
        Assert.DoesNotContain("IX-PILOT-E", codes);
        Assert.DoesNotContain("IX-REJ-F", codes);
    }

    [Fact]
    public async Task Ideas_AsEvaluator_SeesOnlyAssignedIdea()
    {
        var res = await ClientFor("eval1").GetAsync("/api/ideas?pageSize=100");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("total").GetInt32());
        var codes = Codes(body);
        Assert.Equal(new HashSet<string> { "IX-COMM-D" }, codes);
    }

    [Fact]
    public async Task Ideas_AsJudge_SeesOnlyFinalistStatusIdeas()
    {
        var res = await ClientFor("judge1").GetAsync("/api/ideas?pageSize=100");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, body.GetProperty("total").GetInt32());
        var codes = Codes(body);
        Assert.Equal(new HashSet<string> { "IX-APPR-C", "IX-COMM-D", "IX-PILOT-E" }, codes);
    }

    [Fact]
    public async Task Ideas_AsAdmin_StatusThemeQAndStageFiltersApply()
    {
        var byStatus = await (await ClientFor("admin1").GetAsync("/api/ideas?status=approved")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, byStatus.GetProperty("total").GetInt32());
        Assert.Equal(new HashSet<string> { "IX-APPR-C" }, Codes(byStatus));

        var byTheme = await (await ClientFor("admin1").GetAsync($"/api/ideas?strategicThemeId={_themeOneId}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, byTheme.GetProperty("total").GetInt32());
        Assert.Equal(new HashSet<string> { "IX-DRAFT-A", "IX-APPR-C", "IX-PILOT-E" }, Codes(byTheme));

        var byQ = await (await ClientFor("admin1").GetAsync("/api/ideas?q=APPR")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, byQ.GetProperty("total").GetInt32());
        Assert.Equal(new HashSet<string> { "IX-APPR-C" }, Codes(byQ));

        var byStage = await (await ClientFor("admin1").GetAsync("/api/ideas?stage=2")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, byStage.GetProperty("total").GetInt32());
        Assert.Equal(new HashSet<string> { "IX-APPR-C", "IX-COMM-D" }, Codes(byStage));
    }

    [Fact]
    public async Task Ideas_AsAdmin_PagingReturnsTotalAndCapsItems()
    {
        var res = await ClientFor("admin1").GetAsync("/api/ideas?page=1&pageSize=2");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(6, body.GetProperty("total").GetInt32());
        Assert.Equal(1, body.GetProperty("page").GetInt32());
        Assert.Equal(2, body.GetProperty("pageSize").GetInt32());
        Assert.Equal(2, body.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Ideas_AsSubmitter_CannotSeeUnrelatedIdeaEvenViaMatchingFilter()
    {
        // IX-COMM-D belongs to sub2 (no email-team relation to sub1) and is in 'committee' status;
        // sub1 filtering by that exact status must still yield zero results.
        var res = await ClientFor("sub1").GetAsync("/api/ideas?status=committee");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, body.GetProperty("total").GetInt32());

        // and by theme3 (IX-COMM-D's theme, not shared with sub1's ideas)
        var res2 = await ClientFor("sub1").GetAsync($"/api/ideas?strategicThemeId={_themeThreeId}");
        var body2 = await res2.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, body2.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Ideas_AsSubmitterWithEmptyEmail_DoesNotMatchEmptyEmailTeamMembers_ButSeesOwnIdeas()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
            var subRole = db.Roles.Single(r => r.Code == RoleCodes.Submitter);
            var draftStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Draft);
            var sub2 = db.Users.Single(u => u.SamAccountName == "sub2");

            // submitter whose Email is empty (matches the FakeAdIdentityLookupService "sub3" identity, which also has an empty email)
            var sub3 = new User { Id = Guid.NewGuid(), SamAccountName = "sub3", Email = "", FullNameAr = "م3", FullNameEn = "Sub Three", Department = "IT" };
            db.Users.Add(sub3);
            db.SaveChanges();
            db.Set<UserRole>().Add(new UserRole { UserId = sub3.Id, RoleId = subRole.Id, IsPrimary = true });
            db.SaveChanges();

            // sub3's own idea -- must still be visible to sub3
            var ownIdea = new Idea
            {
                Id = Guid.NewGuid(), Code = "IX-OWN-G", TitleAr = "ع", TitleEn = "IX-OWN-G",
                ProblemStatementAr = "م", ProblemStatementEn = "P", ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
                ExpectedBenefitsAr = "ب", ExpectedBenefitsEn = "B",
                StrategicThemeId = _themeOneId, SubmitterId = sub3.Id, IdeaStatusId = draftStatus.Id,
                ParticipationType = "individual", CurrentStage = 0,
            };

            // sub2's idea with a team member whose Email is empty -- an empty caller email must NOT match this
            var emptyEmailIdea = new Idea
            {
                Id = Guid.NewGuid(), Code = "IX-EMPTY-H", TitleAr = "ع", TitleEn = "IX-EMPTY-H",
                ProblemStatementAr = "م", ProblemStatementEn = "P", ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
                ExpectedBenefitsAr = "ب", ExpectedBenefitsEn = "B",
                StrategicThemeId = _themeOneId, SubmitterId = sub2.Id, IdeaStatusId = draftStatus.Id,
                ParticipationType = "team", TeamName = "Empty Team", CurrentStage = 0,
            };
            db.Ideas.AddRange(ownIdea, emptyEmailIdea);
            db.SaveChanges();

            // bypasses normal validation (which would reject a blank email) by inserting directly via db
            db.IdeaTeamMembers.Add(new IdeaTeamMember { Id = Guid.NewGuid(), IdeaId = emptyEmailIdea.Id, Name = "Empty Email Member", Email = "", SortOrder = 0 });
            db.SaveChanges();
        }

        var res = await ClientFor("sub3").GetAsync("/api/ideas?pageSize=100");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        var codes = Codes(body);

        Assert.Contains("IX-OWN-G", codes);
        Assert.DoesNotContain("IX-EMPTY-H", codes);
    }

    [Theory]
    [InlineData(500, 25)]
    [InlineData(0, 25)]
    public async Task Ideas_PageSizeOutOfRange_IsClampedTo25(int requestedPageSize, int expectedPageSize)
    {
        var res = await ClientFor("admin1").GetAsync($"/api/ideas?pageSize={requestedPageSize}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expectedPageSize, body.GetProperty("pageSize").GetInt32());
    }

    public void Dispose() => _connection.Dispose();
}
