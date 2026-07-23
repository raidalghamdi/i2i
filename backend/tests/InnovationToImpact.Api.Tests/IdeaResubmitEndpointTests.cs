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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace InnovationToImpact.Api.Tests;

public class IdeaResubmitEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"idea-resubmit-endpoint-test-{Guid.NewGuid():N}");
    private Guid _submitterId;
    private Guid _themeId;
    private Guid _activityId;

    public IdeaResubmitEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("submitter1", "Submitter One", "submitter1@gac-demo.sa", null, null, null),
        });

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            // Override DevAuth:SamAccountName to empty so a request with no X-Dev-User header
            // genuinely fails authentication (401) instead of DevAuthenticationHandler falling
            // back to the "devuser" default from appsettings.Development.json, which would
            // auto-authenticate and then fail identity resolution (503) since this fixture's
            // FakeAdIdentityLookupService only knows about "submitter1". See the identical
            // pattern/rationale in AuthorizationPolicyTests.cs.
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

                services.Configure<EvidenceStorageOptions>(options => options.RootPath = _rootPath);

                using var scope = services.BuildServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
                db.Database.EnsureCreated();
                Seed(db);
            });
        });
    }

    private void Seed(InnovationDbContext db)
    {
        var roleId = db.Roles.Single(r => r.Code == "submitter").Id;
        _submitterId = Guid.NewGuid();
        db.Users.Add(new User { Id = _submitterId, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "s1", FullNameEn = "s1" });
        db.SaveChanges();
        db.Set<UserRole>().Add(new UserRole { UserId = _submitterId, RoleId = roleId, IsPrimary = true });
        db.SaveChanges();

        _themeId = db.StrategicThemes.First().Id;
        _activityId = Guid.NewGuid();
        db.Activities.Add(new Activity { Id = _activityId, NameAr = "ف", NameEn = "Activity", Type = "hackathon", Status = "open", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30), CreatedById = _submitterId });
        db.SaveChanges();
    }

    [Fact]
    public async Task Resubmit_IdeaNotReturned_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var createResponse = await client.PostAsJsonAsync("/api/ideas", new
        {
            titleAr = "ا", titleEn = "T", problemStatementAr = "م", problemStatementEn = "P",
            proposedSolutionAr = "ح", proposedSolutionEn = "S", expectedBenefitsAr = "ف", expectedBenefitsEn = "B",
            strategicThemeId = _themeId, activityId = _activityId, challengeId = (Guid?)null,
            participationType = "individual", teamName = (string?)null, teamMembers = Array.Empty<object>(),
            ipAcknowledged = true, termsAgreed = true,
        });
        var location = createResponse.Headers.Location!.ToString();
        var ideaId = Guid.Parse(location.Split('/').Last());

        var resubmitResponse = await client.PostAsJsonAsync($"/api/ideas/{ideaId}/resubmit", new
        {
            titleAr = "ا", titleEn = "Updated", proposedSolutionAr = "ح", proposedSolutionEn = "S",
            activityId = _activityId, strategicThemeId = _themeId, challengeId = (Guid?)null,
            participationType = "individual", teamName = (string?)null, teamMembers = Array.Empty<object>(),
        });

        Assert.Equal(HttpStatusCode.BadRequest, resubmitResponse.StatusCode);
    }

    [Fact]
    public async Task Resubmit_AnonymousUser_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync($"/api/ideas/{Guid.NewGuid()}/resubmit", new
        {
            titleAr = "ا", titleEn = "Updated", proposedSolutionAr = "ح", proposedSolutionEn = "S",
            activityId = _activityId, strategicThemeId = _themeId, challengeId = (Guid?)null,
            participationType = "individual", teamName = (string?)null, teamMembers = Array.Empty<object>(),
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public void Dispose() => _connection.Dispose();
}
