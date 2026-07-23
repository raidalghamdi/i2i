using System.Net;
using System.Net.Http.Json;
using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Auth;
using InnovationToImpact.Infrastructure.Data;
using InnovationToImpact.Infrastructure.Ideas;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace InnovationToImpact.Api.Tests;

public class IdeasEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"evidence-storage-test-{Guid.NewGuid():N}");
    private Guid _submitter1Id;
    private Guid _submitter2Id;
    private Guid _strategicThemeId;
    private Guid _activityId;

    public IdeasEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("submitter1", "Submitter One", "submitter1@gac-demo.sa", null, null, null),
            new AdIdentity("submitter2", "Submitter Two", "submitter2@gac-demo.sa", null, null, null),
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

                services.Configure<EvidenceStorageOptions>(options => options.RootPath = _rootPath);

                using var scope = services.BuildServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
                db.Database.EnsureCreated();
                SeedUsersAndRoles(db);
            });
        });
    }

    private void SeedUsersAndRoles(InnovationDbContext db)
    {
        var roleIds = db.Roles.ToDictionary(r => r.Code, r => r.Id);

        _submitter1Id = Guid.NewGuid();
        db.Users.Add(new User { Id = _submitter1Id, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "submitter1", FullNameEn = "submitter1" });
        _submitter2Id = Guid.NewGuid();
        db.Users.Add(new User { Id = _submitter2Id, SamAccountName = "submitter2", Email = "submitter2@gac-demo.sa", FullNameAr = "submitter2", FullNameEn = "submitter2" });
        db.SaveChanges();

        db.Set<UserRole>().Add(new UserRole { UserId = _submitter1Id, RoleId = roleIds[RoleCodes.Submitter], IsPrimary = true });
        db.Set<UserRole>().Add(new UserRole { UserId = _submitter2Id, RoleId = roleIds[RoleCodes.Submitter], IsPrimary = true });
        db.SaveChanges();

        _strategicThemeId = db.StrategicThemes.First().Id;
        _activityId = Guid.NewGuid();
        db.Activities.Add(new Activity { Id = _activityId, NameAr = "فعالية", NameEn = "Activity", Type = "hackathon", Status = "open", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30), CreatedById = _submitter1Id });
        db.SaveChanges();
    }

    private static object MakeIdeaBody(Guid strategicThemeId, Guid activityId) => new
    {
        titleAr = "فكرة",
        titleEn = "Idea",
        problemStatementAr = "مشكلة",
        problemStatementEn = "Problem",
        proposedSolutionAr = "حل",
        proposedSolutionEn = "Solution",
        expectedBenefitsAr = "فوائد",
        expectedBenefitsEn = "Benefits",
        strategicThemeId,
        activityId,
        challengeId = (Guid?)null,
        participationType = "individual",
        teamName = (string?)null,
        teamMembers = Array.Empty<object>(),
        ipAcknowledged = true,
        termsAgreed = true,
    };

    /// <summary>
    /// Extracts the created idea's id from the Location header set by Results.Created(...) rather than
    /// deserializing the JSON body — sidesteps a real System.Text.Json pitfall: the default
    /// JsonSerializerOptions used by ReadFromJsonAsync&lt;T&gt; is NOT case-insensitive, so a camelCase
    /// response body ("id", "code") deserialized into a PascalCase record (Id, Code) can silently leave
    /// every property at its default value instead of throwing. This is the same defensive choice this
    /// codebase's earlier AuditLogReportEndpointTests made (raw string checks instead of typed deserialization).
    /// </summary>
    private static Guid ExtractIdFromLocation(HttpResponseMessage response)
    {
        var location = response.Headers.Location!.ToString();
        return Guid.Parse(location.Split('/').Last());
    }

    [Fact]
    public async Task CreateThenGetById_ReturnsCreatedIdea()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var createResponse = await client.PostAsJsonAsync("/api/ideas", MakeIdeaBody(_strategicThemeId, _activityId));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var ideaId = ExtractIdFromLocation(createResponse);

        var getResponse = await client.GetAsync($"/api/ideas/{ideaId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task Create_WithNonexistentStrategicTheme_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var createResponse = await client.PostAsJsonAsync("/api/ideas", MakeIdeaBody(Guid.NewGuid(), _activityId));

        Assert.Equal(HttpStatusCode.BadRequest, createResponse.StatusCode);
    }

    [Fact]
    public async Task GetById_WrongOwner_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");
        var createResponse = await client.PostAsJsonAsync("/api/ideas", MakeIdeaBody(_strategicThemeId, _activityId));
        var ideaId = ExtractIdFromLocation(createResponse);

        var otherClient = _factory.CreateClient();
        otherClient.DefaultRequestHeaders.Add("X-Dev-User", "submitter2");
        var getResponse = await otherClient.GetAsync($"/api/ideas/{ideaId}");

        Assert.Equal(HttpStatusCode.Forbidden, getResponse.StatusCode);
    }

    [Fact]
    public async Task UploadAttachmentThenSubmit_Succeeds()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");
        var createResponse = await client.PostAsJsonAsync("/api/ideas", MakeIdeaBody(_strategicThemeId, _activityId));
        var ideaId = ExtractIdFromLocation(createResponse);

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "evidence.pdf");

        var uploadResponse = await client.PostAsync($"/api/ideas/{ideaId}/attachments", content);
        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);

        var submitResponse = await client.PostAsync($"/api/ideas/{ideaId}/submit", null);
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);
    }

    [Fact]
    public async Task Submit_WithoutAttachment_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");
        var createResponse = await client.PostAsJsonAsync("/api/ideas", MakeIdeaBody(_strategicThemeId, _activityId));
        var ideaId = ExtractIdFromLocation(createResponse);

        var submitResponse = await client.PostAsync($"/api/ideas/{ideaId}/submit", null);

        Assert.Equal(HttpStatusCode.BadRequest, submitResponse.StatusCode);
    }

    public void Dispose()
    {
        _connection.Dispose();
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
