using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Ideas;
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

public class PillarDrilldownEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"pillar-drilldown-endpoint-test-{Guid.NewGuid():N}");

    private Guid _theme0Id;
    private Guid _theme1Id;
    private Guid _ownerId;

    public PillarDrilldownEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var lookup = new FakeAdIdentityLookupService(
            seedIdentities: new[]
            {
                new AdIdentity("admin1", "Admin One", "admin1@gac-demo.sa", null, null, null),
                new AdIdentity("supervisor1", "Supervisor One", "supervisor1@gac-demo.sa", null, null, null),
                new AdIdentity("judge1", "Judge One", "judge1@gac-demo.sa", null, null, null),
                new AdIdentity("submitter1", "Submitter One", "submitter1@gac-demo.sa", null, null, null),
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

                SeedData(db);
            });
        });
    }

    private void SeedData(InnovationDbContext db)
    {
        var adminRoleId = db.Roles.Single(r => r.Code == RoleCodes.Admin).Id;
        var supervisorRoleId = db.Roles.Single(r => r.Code == RoleCodes.Supervisor).Id;
        var judgeRoleId = db.Roles.Single(r => r.Code == RoleCodes.Judge).Id;
        var submitterRoleId = db.Roles.Single(r => r.Code == RoleCodes.Submitter).Id;

        var adminId = Guid.NewGuid();
        var supervisorId = Guid.NewGuid();
        var judgeId = Guid.NewGuid();
        var submitter1Id = Guid.NewGuid();

        db.Users.AddRange(
            new User { Id = adminId, SamAccountName = "admin1", Email = "admin1@gac-demo.sa", FullNameAr = "admin1", FullNameEn = "admin1" },
            new User { Id = supervisorId, SamAccountName = "supervisor1", Email = "supervisor1@gac-demo.sa", FullNameAr = "supervisor1", FullNameEn = "supervisor1" },
            new User { Id = judgeId, SamAccountName = "judge1", Email = "judge1@gac-demo.sa", FullNameAr = "judge1", FullNameEn = "judge1" },
            new User { Id = submitter1Id, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "submitter1", FullNameEn = "submitter1" });
        db.SaveChanges();

        db.Set<UserRole>().AddRange(
            new UserRole { UserId = adminId, RoleId = adminRoleId, IsPrimary = true },
            new UserRole { UserId = supervisorId, RoleId = supervisorRoleId, IsPrimary = true },
            new UserRole { UserId = judgeId, RoleId = judgeRoleId, IsPrimary = true },
            new UserRole { UserId = submitter1Id, RoleId = submitterRoleId, IsPrimary = true });
        db.SaveChanges();

        var themes = db.StrategicThemes.OrderBy(t => t.Priority).ToList();
        _theme0Id = themes[0].Id;
        _theme1Id = themes[1].Id;
        _ownerId = themes[0].OwnerId;

        var statuses = db.IdeaStatuses.ToDictionary(s => s.Code, s => s.Id);

        var now = DateTime.UtcNow;

        Idea MakeIdea(string code, string statusCode, int currentStage, Guid themeId, DateTime createdAt)
        {
            var idea = new Idea
            {
                Id = Guid.NewGuid(),
                Code = code,
                TitleAr = "ا-" + code, TitleEn = "T-" + code, ProblemStatementAr = "م", ProblemStatementEn = "P",
                ProposedSolutionAr = "ح", ProposedSolutionEn = "S", ExpectedBenefitsAr = "ف", ExpectedBenefitsEn = "B",
                StrategicThemeId = themeId,
                IdeaStatusId = statuses[statusCode],
                SubmitterId = submitter1Id,
                CurrentStage = currentStage,
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
            };
            db.Ideas.Add(idea);
            return idea;
        }

        // theme0: 6 ideas across stages/statuses.
        var ideaA = MakeIdea("PIL-A", IdeaStatusCodes.Draft, 0, _theme0Id, now);
        var ideaB = MakeIdea("PIL-B", IdeaStatusCodes.Submitted, 1, _theme0Id, now);
        var ideaC = MakeIdea("PIL-C", IdeaStatusCodes.Approved, 5, _theme0Id, now);
        var ideaD = MakeIdea("PIL-D", IdeaStatusCodes.InPilot, 6, _theme0Id, now);        // stage>=6, non-terminal -> pilotsActive
        var ideaE = MakeIdea("PIL-E", IdeaStatusCodes.InMeasurement, 7, _theme0Id, now);  // stage>=6, non-terminal -> pilotsActive + implementationsDone
        var ideaF = MakeIdea("PIL-F", IdeaStatusCodes.InScaling, 8, _theme0Id, now);      // stage>=6, non-terminal -> pilotsActive + implementationsDone
        var ideaRejected = MakeIdea("PIL-G", IdeaStatusCodes.Rejected, 6, _theme0Id, now); // stage>=6 but terminal -> NOT pilotsActive

        // theme1: 2 ideas (should not affect theme0's KPIs).
        MakeIdea("PIL-H", IdeaStatusCodes.Submitted, 1, _theme1Id, now);
        MakeIdea("PIL-I", IdeaStatusCodes.InPilot, 6, _theme1Id, now);
        db.SaveChanges();

        var financialCategoryId = db.BenefitCategories.Single(c => c.Code == "financial").Id;
        var operationalCategoryId = db.BenefitCategories.Single(c => c.Code == "operational").Id;
        var quantitativeTypeId = db.BenefitTypes.Single(t => t.Code == "quantitative").Id;

        db.Benefits.AddRange(
            new Benefit
            {
                Id = Guid.NewGuid(),
                IdeaId = ideaC.Id,
                TitleAr = "أ", TitleEn = "Financial Benefit 1",
                BenefitTypeId = quantitativeTypeId,
                BenefitCategoryId = financialCategoryId,
                TargetValue = 20000m,
                RealizedValue = 15000.50m,
            },
            new Benefit
            {
                Id = Guid.NewGuid(),
                IdeaId = ideaD.Id,
                TitleAr = "ب", TitleEn = "Financial Benefit 2",
                BenefitTypeId = quantitativeTypeId,
                BenefitCategoryId = financialCategoryId,
                TargetValue = 1000m,
                RealizedValue = 500.25m,
            },
            new Benefit
            {
                Id = Guid.NewGuid(),
                IdeaId = ideaE.Id,
                TitleAr = "ج", TitleEn = "Operational Benefit",
                BenefitTypeId = quantitativeTypeId,
                BenefitCategoryId = operationalCategoryId,
                TargetValue = 999m,
                RealizedValue = 999m,
            });
        db.SaveChanges();
    }

    [Fact]
    public async Task Get_AsSupervisor_ReturnsDetail()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.GetAsync($"/api/analytics/pillars/{_theme0Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(_theme0Id, body.GetProperty("themeId").GetGuid());
        Assert.True(body.TryGetProperty("nameAr", out _));
        Assert.True(body.TryGetProperty("nameEn", out _));
        Assert.True(body.TryGetProperty("kpis", out _));
        Assert.True(body.TryGetProperty("timeline", out _));
        Assert.True(body.TryGetProperty("ideas", out _));
    }

    [Fact]
    public async Task Get_AsSubmitter_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.GetAsync($"/api/analytics/pillars/{_theme0Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_UnknownTheme_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.GetAsync($"/api/analytics/pillars/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Kpis_ComputeIdeasBudgetPilotsAndImplementations()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var body = await client.GetFromJsonAsync<JsonElement>($"/api/analytics/pillars/{_theme0Id}");
        var kpis = body.GetProperty("kpis");

        // theme0 has 7 ideas seeded (A..G).
        Assert.Equal(7, kpis.GetProperty("ideas").GetInt32());

        // Only financial-category benefits count: 15000.50 (C) + 500.25 (D) = 15500.75; operational (E) excluded.
        Assert.Equal(15500.75m, kpis.GetProperty("budgetSpent").GetDecimal());
        Assert.Equal(21000m, kpis.GetProperty("budgetAllocated").GetDecimal());

        // pilotsActive: stage>=6 and not terminal -> D, E, F (G is rejected/terminal, excluded).
        Assert.Equal(3, kpis.GetProperty("pilotsActive").GetInt32());

        // implementationsDone: status in {in_measurement, in_scaling} -> E, F.
        Assert.Equal(2, kpis.GetProperty("implementationsDone").GetInt32());
    }

    [Fact]
    public async Task Timeline_HasTwelveMonths()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var body = await client.GetFromJsonAsync<JsonElement>($"/api/analytics/pillars/{_theme0Id}");
        var timeline = body.GetProperty("timeline").EnumerateArray().ToList();

        Assert.Equal(12, timeline.Count);
        Assert.Equal(7, timeline.Sum(t => t.GetProperty("count").GetInt32()));
    }

    [Fact]
    public async Task Ideas_OrderedByStageDescending_AndScopedToTheme()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var body = await client.GetFromJsonAsync<JsonElement>($"/api/analytics/pillars/{_theme0Id}");
        var ideas = body.GetProperty("ideas").EnumerateArray().ToList();

        Assert.Equal(7, ideas.Count);
        var stages = ideas.Select(i => i.GetProperty("currentStage").GetInt32()).ToList();
        Assert.Equal(stages.OrderByDescending(s => s).ToList(), stages);

        var codes = ideas.Select(i => i.GetProperty("code").GetString()).ToList();
        Assert.DoesNotContain("PIL-H", codes);
        Assert.DoesNotContain("PIL-I", codes);
    }

    public void Dispose()
    {
        _connection.Dispose();
        if (Directory.Exists(_rootPath)) Directory.Delete(_rootPath, recursive: true);
    }
}
