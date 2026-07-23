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

public class PublicSearchEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private Guid _themeId;

    public PublicSearchEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
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

        var theme = new StrategicTheme { Id = Guid.NewGuid(), NameAr = "مسار الاستدامة", NameEn = "Sustainability Track", DescriptionAr = "وصف", DescriptionEn = "Green energy theme", Priority = 100, OwnerId = ownerId };
        db.StrategicThemes.Add(theme);
        _themeId = theme.Id;
        db.SaveChanges();

        var approvedStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Approved);
        var draftStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Draft);

        // Public-safe (approved) idea whose TITLE matches "solar".
        db.Ideas.Add(new Idea
        {
            Id = Guid.NewGuid(), Code = "IDEA-SOLAR", TitleAr = "الطاقة الشمسية", TitleEn = "Solar Panels",
            ProblemStatementAr = "م", ProblemStatementEn = "P", ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ب", ExpectedBenefitsEn = "B",
            StrategicThemeId = theme.Id, SubmitterId = ownerId, IdeaStatusId = approvedStatus.Id, ParticipationType = "individual",
        });
        // Public-safe idea whose PROBLEM STATEMENT matches "solar" (title does not).
        db.Ideas.Add(new Idea
        {
            Id = Guid.NewGuid(), Code = "IDEA-PROBLEM", TitleAr = "عنوان", TitleEn = "Unrelated Title",
            ProblemStatementAr = "م", ProblemStatementEn = "Cities lack solar coverage", ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ب", ExpectedBenefitsEn = "B",
            StrategicThemeId = theme.Id, SubmitterId = ownerId, IdeaStatusId = approvedStatus.Id, ParticipationType = "individual",
        });
        // DRAFT idea that also matches "solar" — MUST be excluded (privacy invariant).
        db.Ideas.Add(new Idea
        {
            Id = Guid.NewGuid(), Code = "IDEA-DRAFT-SOLAR", TitleAr = "مسودة", TitleEn = "Solar Draft",
            ProblemStatementAr = "م", ProblemStatementEn = "P", ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ب", ExpectedBenefitsEn = "B",
            StrategicThemeId = theme.Id, SubmitterId = ownerId, IdeaStatusId = draftStatus.Id, ParticipationType = "individual",
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task Search_Anonymous_MatchesIdeaTitleAndProblem_ExcludesDraft()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/public/search?q=solar");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;
        var codes = body.GetProperty("ideas").EnumerateArray().Select(i => i.GetProperty("code").GetString()).ToList();
        Assert.Contains("IDEA-SOLAR", codes);       // title match
        Assert.Contains("IDEA-PROBLEM", codes);      // problem-statement match
        Assert.DoesNotContain("IDEA-DRAFT-SOLAR", codes); // draft excluded by public-safe filter
    }

    [Fact]
    public async Task Search_Anonymous_MatchesTrackNameAndDescription()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/public/search?q=green");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;
        var trackNames = body.GetProperty("tracks").EnumerateArray().Select(t => t.GetProperty("nameEn").GetString()).ToList();
        Assert.Contains("Sustainability Track", trackNames); // matched via DescriptionEn "Green energy theme"
    }

    [Fact]
    public async Task Search_CaseInsensitive()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/public/search?q=SOLAR");
        var body = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;
        var codes = body.GetProperty("ideas").EnumerateArray().Select(i => i.GetProperty("code").GetString()).ToList();
        Assert.Contains("IDEA-SOLAR", codes);
    }

    [Fact]
    public async Task Search_BlankQuery_ReturnsEmptyGroups()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/public/search?q=%20");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;
        Assert.Empty(body.GetProperty("ideas").EnumerateArray());
        Assert.Empty(body.GetProperty("tracks").EnumerateArray());
    }

    [Fact]
    public async Task Search_MissingQuery_ReturnsEmptyGroups()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/public/search");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;
        Assert.Empty(body.GetProperty("ideas").EnumerateArray());
        Assert.Empty(body.GetProperty("tracks").EnumerateArray());
    }

    [Fact]
    public async Task Search_WildcardChars_TreatedLiterally_NoMatchExplosion()
    {
        var client = _factory.CreateClient();
        // "%" must be escaped, not treated as LIKE wildcard (which would match every idea).
        var res = await client.GetAsync("/api/public/search?q=%25");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;
        Assert.Empty(body.GetProperty("ideas").EnumerateArray());
    }

    public void Dispose() => _connection.Dispose();
}
