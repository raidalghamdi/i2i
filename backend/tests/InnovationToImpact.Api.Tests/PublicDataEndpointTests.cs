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

public class PublicDataEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private Guid _themeId;
    private Guid _activityId;
    private Guid _approvedIdeaId;
    private Guid _draftIdeaId;
    private Guid _inPilotIdeaId;

    public PublicDataEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        // Null seeds the fake's built-in "devuser" default identity, matching appsettings.Development.json's
        // DevAuth:SamAccountName fallback used when no X-Dev-User header is sent (Array.Empty would remove it
        // and cause identity resolution to fail with a 503 before the anonymous endpoint is ever reached).
        var lookup = new FakeAdIdentityLookupService(null);
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
        var ownerId = Guid.NewGuid();
        db.Users.Add(new User { Id = ownerId, SamAccountName = "owner1", Email = "owner1@gac-demo.sa", FullNameAr = "م", FullNameEn = "Owner" });
        db.SaveChanges();

        // Priorities set well above the pre-seeded StrategicThemes (which use priorities 1-3) so these two
        // sort deterministically last (and relative to each other) under ListTracksAsync's OrderBy(Priority).
        var theme1 = new StrategicTheme { Id = Guid.NewGuid(), NameAr = "مسار1", NameEn = "Track One", DescriptionAr = "وصف1", DescriptionEn = "Desc One", Priority = 100, OwnerId = ownerId };
        var theme2 = new StrategicTheme { Id = Guid.NewGuid(), NameAr = "مسار2", NameEn = "Track Two", DescriptionAr = "وصف2", DescriptionEn = "Desc Two", Priority = 101, OwnerId = ownerId };
        db.StrategicThemes.Add(theme1);
        db.StrategicThemes.Add(theme2);
        _themeId = theme1.Id;

        var activity = new Activity
        {
            Id = Guid.NewGuid(), NameAr = "نشاط", NameEn = "Activity One", Type = "event", Status = "open",
            StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddDays(30), CreatedById = ownerId,
        };
        db.Activities.Add(activity);
        _activityId = activity.Id;

        var activeChallenge = new Challenge { Id = Guid.NewGuid(), StrategicThemeId = theme1.Id, TextAr = "تحدي نشط", TextEn = "Active Challenge", SortOrder = 1, IsActive = true };
        var inactiveChallenge = new Challenge { Id = Guid.NewGuid(), StrategicThemeId = theme1.Id, TextAr = "تحدي غير نشط", TextEn = "Inactive Challenge", SortOrder = 2, IsActive = false };
        db.Challenges.Add(activeChallenge);
        db.Challenges.Add(inactiveChallenge);

        db.SaveChanges();

        var approvedStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Approved);
        var draftStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Draft);
        var inPilotStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.InPilot);

        _approvedIdeaId = Guid.NewGuid();
        db.Ideas.Add(new Idea
        {
            Id = _approvedIdeaId, Code = "IDEA-APPROVED", TitleAr = "فكرة معتمدة", TitleEn = "Approved Idea",
            ProblemStatementAr = "م", ProblemStatementEn = "P", ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ب", ExpectedBenefitsEn = "B",
            StrategicThemeId = theme1.Id, ActivityId = activity.Id, SubmitterId = ownerId,
            IdeaStatusId = approvedStatus.Id, ParticipationType = "individual",
        });

        _draftIdeaId = Guid.NewGuid();
        db.Ideas.Add(new Idea
        {
            Id = _draftIdeaId, Code = "IDEA-DRAFT", TitleAr = "فكرة مسودة", TitleEn = "Draft Idea",
            ProblemStatementAr = "م", ProblemStatementEn = "P", ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ب", ExpectedBenefitsEn = "B",
            StrategicThemeId = theme1.Id, ActivityId = activity.Id, SubmitterId = ownerId,
            IdeaStatusId = draftStatus.Id, ParticipationType = "individual",
        });

        _inPilotIdeaId = Guid.NewGuid();
        db.Ideas.Add(new Idea
        {
            Id = _inPilotIdeaId, Code = "IDEA-PILOT", TitleAr = "فكرة تجريبية", TitleEn = "Piloting Idea",
            ProblemStatementAr = "م", ProblemStatementEn = "P", ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ب", ExpectedBenefitsEn = "B",
            StrategicThemeId = theme1.Id, ActivityId = activity.Id, SubmitterId = ownerId,
            IdeaStatusId = inPilotStatus.Id, ParticipationType = "individual",
        });

        db.SaveChanges();
    }

    [Fact]
    public async Task ListTracks_Anonymous_ReturnsBothThemesOrderedByPriority()
    {
        var client = _factory.CreateClient(); // NO X-Dev-User header — anonymous
        var res = await client.GetAsync("/api/public/tracks");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;
        // The DB already seeds 3 default StrategicThemes; assert our two seeded tracks are present and,
        // since their priorities (100, 101) sort after everything else, that they appear adjacent and ordered.
        var names = body.EnumerateArray().Select(t => t.GetProperty("nameEn").GetString()).ToList();
        Assert.Contains("Track One", names);
        Assert.Contains("Track Two", names);
        Assert.True(names.IndexOf("Track One") < names.IndexOf("Track Two"));
    }

    [Fact]
    public async Task GetTrack_Anonymous_IncludesApprovedIdeaAndActiveChallengeOnly()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync($"/api/public/tracks/{_themeId}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;

        var ideaCodes = body.GetProperty("ideas").EnumerateArray().Select(i => i.GetProperty("code").GetString()).ToList();
        Assert.Contains("IDEA-APPROVED", ideaCodes);
        Assert.DoesNotContain("IDEA-DRAFT", ideaCodes);

        var challengeTexts = body.GetProperty("challenges").EnumerateArray().Select(c => c.GetProperty("textEn").GetString()).ToList();
        Assert.Contains("Active Challenge", challengeTexts);
        Assert.DoesNotContain("Inactive Challenge", challengeTexts);
    }

    [Fact]
    public async Task GetTrack_MissingId_Returns404()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync($"/api/public/tracks/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task ListActivities_Anonymous_IdeaCountCountsOnlyPublicSafeIdeas()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/public/activities");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;
        var activity = body.EnumerateArray().Single(a => a.GetProperty("id").GetGuid() == _activityId);
        // Approved + in_pilot are both public-safe statuses; the draft idea is excluded.
        Assert.Equal(2, activity.GetProperty("ideaCount").GetInt32());
    }

    [Fact]
    public async Task GetActivity_Anonymous_ExcludesDraftIdea()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync($"/api/public/activities/{_activityId}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;

        var ideaCodes = body.GetProperty("ideas").EnumerateArray().Select(i => i.GetProperty("code").GetString()).ToList();
        Assert.Contains("IDEA-APPROVED", ideaCodes);
        Assert.Contains("IDEA-PILOT", ideaCodes);
        Assert.DoesNotContain("IDEA-DRAFT", ideaCodes);
        Assert.Equal(2, body.GetProperty("activity").GetProperty("ideaCount").GetInt32());
        Assert.Equal(1, body.GetProperty("approvedCount").GetInt32());
        Assert.Equal(1, body.GetProperty("pilotingCount").GetInt32());
    }

    [Fact]
    public async Task GetActivity_MissingId_Returns404()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync($"/api/public/activities/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    public void Dispose() => _connection.Dispose();
}
