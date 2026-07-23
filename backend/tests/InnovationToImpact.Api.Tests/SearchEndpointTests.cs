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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace InnovationToImpact.Api.Tests;

public class SearchEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;

    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid SubmitterId = Guid.NewGuid();
    private static readonly Guid OtherSubmitterId = Guid.NewGuid();

    public SearchEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("admin1", "Admin One", "admin1@gac-demo.sa", null, null, null),
            new AdIdentity("submitter1", "Submitter One", "submitter1@gac-demo.sa", null, null, null),
            new AdIdentity("othersubmitter1", "Other Submitter", "othersubmitter1@gac-demo.sa", null, null, null),
        });

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DevAuth:SamAccountName"] = "",
                }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<InnovationDbContext>>();
                services.AddDbContext<InnovationDbContext>(options => options.UseSqlite(_connection));

                services.RemoveAll<IAdIdentityLookupService>();
                services.AddSingleton<IAdIdentityLookupService>(lookup);

                using var scope = services.BuildServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
                db.Database.EnsureCreated();
                SeedData(db);
            });
        });
    }

    private static void SeedData(InnovationDbContext db)
    {
        var roleIds = db.Roles.ToDictionary(r => r.Code, r => r.Id);

        db.Users.Add(new User { Id = AdminId, SamAccountName = "admin1", Email = "admin1@gac-demo.sa", FullNameAr = "المدير الأول", FullNameEn = "Admin One" });
        db.Users.Add(new User { Id = SubmitterId, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "مقدم الأول", FullNameEn = "Submitter One" });
        db.Users.Add(new User { Id = OtherSubmitterId, SamAccountName = "othersubmitter1", Email = "othersubmitter1@gac-demo.sa", FullNameAr = "مقدم آخر", FullNameEn = "Other Submitter" });
        db.SaveChanges();

        db.Set<UserRole>().Add(new UserRole { UserId = AdminId, RoleId = roleIds[RoleCodes.Admin], IsPrimary = true });
        db.Set<UserRole>().Add(new UserRole { UserId = SubmitterId, RoleId = roleIds[RoleCodes.Submitter], IsPrimary = true });
        db.Set<UserRole>().Add(new UserRole { UserId = OtherSubmitterId, RoleId = roleIds[RoleCodes.Submitter], IsPrimary = true });
        db.SaveChanges();

        var themeId = db.StrategicThemes.First().Id;
        var draftStatusId = db.IdeaStatuses.Single(s => s.Code == "draft").Id;

        // Idea owned by the submitter, matching keyword "Zephyr".
        db.Ideas.Add(new Idea
        {
            Id = Guid.NewGuid(), Code = "IDEA-SRCH-1", TitleAr = "فكرة زفير أولى", TitleEn = "Zephyr Idea One",
            ProblemStatementAr = "م", ProblemStatementEn = "P", ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ف", ExpectedBenefitsEn = "B", StrategicThemeId = themeId,
            IdeaStatusId = draftStatusId, SubmitterId = SubmitterId,
        });

        // Idea owned by a different user, also matching "Zephyr" — should NOT be visible to submitter1.
        db.Ideas.Add(new Idea
        {
            Id = Guid.NewGuid(), Code = "IDEA-SRCH-2", TitleAr = "فكرة زفير ثانية", TitleEn = "Zephyr Idea Two",
            ProblemStatementAr = "م", ProblemStatementEn = "P", ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ف", ExpectedBenefitsEn = "B", StrategicThemeId = themeId,
            IdeaStatusId = draftStatusId, SubmitterId = OtherSubmitterId,
        });
        db.SaveChanges();

        // Challenge matching keyword "Falcon".
        db.Challenges.Add(new Challenge
        {
            Id = Guid.NewGuid(), StrategicThemeId = themeId, TextAr = "تحدي الصقر", TextEn = "Falcon Challenge",
            SortOrder = 1, IsActive = true,
        });
        db.SaveChanges();
    }

    private static void SeedManyIdeas(InnovationDbContext db, int count, string keyword, Guid submitterId, Guid themeId, Guid draftStatusId)
    {
        for (var i = 0; i < count; i++)
        {
            db.Ideas.Add(new Idea
            {
                Id = Guid.NewGuid(), Code = $"IDEA-CAP-{i}", TitleAr = $"فكرة {keyword} {i}", TitleEn = $"{keyword} Idea {i}",
                ProblemStatementAr = "م", ProblemStatementEn = "P", ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
                ExpectedBenefitsAr = "ف", ExpectedBenefitsEn = "B", StrategicThemeId = themeId,
                IdeaStatusId = draftStatusId, SubmitterId = submitterId,
            });
        }
        db.SaveChanges();
    }

    [Fact]
    public async Task Search_AsAdmin_ReturnsIdeasChallengesAndUsers()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await client.GetAsync("/api/search?q=Zephyr");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var ideas = body.GetProperty("ideas");
        Assert.True(ideas.GetArrayLength() >= 2);

        var challengeResponse = await client.GetAsync("/api/search?q=Falcon");
        Assert.Equal(HttpStatusCode.OK, challengeResponse.StatusCode);
        var challengeBody = await challengeResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(challengeBody.GetProperty("challenges").GetArrayLength() > 0);

        var userResponse = await client.GetAsync("/api/search?q=Submitter");
        Assert.Equal(HttpStatusCode.OK, userResponse.StatusCode);
        var userBody = await userResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(userBody.GetProperty("users").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Search_AsSubmitter_ReturnsOnlyOwnIdeasAndNoChallengesOrUsers()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.GetAsync("/api/search?q=Zephyr");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var ideas = body.GetProperty("ideas");
        Assert.Equal(1, ideas.GetArrayLength());
        Assert.Equal("IDEA-SRCH-1", ideas[0].GetProperty("subtitle").GetString());

        Assert.Equal(0, body.GetProperty("challenges").GetArrayLength());
        Assert.Equal(0, body.GetProperty("users").GetArrayLength());
    }

    [Fact]
    public async Task Search_EmptyQuery_ReturnsAllEmpty()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await client.GetAsync("/api/search?q=");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(0, body.GetProperty("ideas").GetArrayLength());
        Assert.Equal(0, body.GetProperty("challenges").GetArrayLength());
        Assert.Equal(0, body.GetProperty("users").GetArrayLength());
    }

    [Fact]
    public async Task Search_ResultsCappedAtFivePerType()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
            var themeId = db.StrategicThemes.First().Id;
            var draftStatusId = db.IdeaStatuses.Single(s => s.Code == "draft").Id;
            SeedManyIdeas(db, 6, "Nebula", AdminId, themeId, draftStatusId);
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await client.GetAsync("/api/search?q=Nebula");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(5, body.GetProperty("ideas").GetArrayLength());
    }

    [Fact]
    public async Task Search_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/search?q=Zephyr");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
