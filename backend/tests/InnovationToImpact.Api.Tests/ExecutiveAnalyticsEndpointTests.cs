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

public class ExecutiveAnalyticsEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"exec-analytics-endpoint-test-{Guid.NewGuid():N}");

    private Guid _theme0Id;
    private Guid _theme1Id;

    public ExecutiveAnalyticsEndpointTests(WebApplicationFactory<Program> factory)
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
        var evaluatorRoleId = db.Roles.Single(r => r.Code == RoleCodes.Evaluator).Id;

        var adminId = Guid.NewGuid();
        var supervisorId = Guid.NewGuid();
        var judgeId = Guid.NewGuid();
        var submitter1Id = Guid.NewGuid();
        var submitter2Id = Guid.NewGuid();
        var evaluatorId = Guid.NewGuid();

        db.Users.AddRange(
            new User { Id = adminId, SamAccountName = "admin1", Email = "admin1@gac-demo.sa", FullNameAr = "admin1", FullNameEn = "admin1" },
            new User { Id = supervisorId, SamAccountName = "supervisor1", Email = "supervisor1@gac-demo.sa", FullNameAr = "supervisor1", FullNameEn = "supervisor1" },
            new User { Id = judgeId, SamAccountName = "judge1", Email = "judge1@gac-demo.sa", FullNameAr = "judge1", FullNameEn = "judge1" },
            new User { Id = submitter1Id, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "submitter1", FullNameEn = "submitter1" },
            new User { Id = submitter2Id, SamAccountName = "submitter2", Email = "submitter2@gac-demo.sa", FullNameAr = "submitter2", FullNameEn = "submitter2" },
            new User { Id = evaluatorId, SamAccountName = "evaluator1", Email = "evaluator1@gac-demo.sa", FullNameAr = "evaluator1", FullNameEn = "evaluator1" });
        db.SaveChanges();

        db.Set<UserRole>().AddRange(
            new UserRole { UserId = adminId, RoleId = adminRoleId, IsPrimary = true },
            new UserRole { UserId = supervisorId, RoleId = supervisorRoleId, IsPrimary = true },
            new UserRole { UserId = judgeId, RoleId = judgeRoleId, IsPrimary = true },
            new UserRole { UserId = submitter1Id, RoleId = submitterRoleId, IsPrimary = true },
            new UserRole { UserId = submitter2Id, RoleId = submitterRoleId, IsPrimary = true },
            new UserRole { UserId = evaluatorId, RoleId = evaluatorRoleId, IsPrimary = true });
        db.SaveChanges();

        var themes = db.StrategicThemes.OrderBy(t => t.Priority).ToList();
        _theme0Id = themes[0].Id;
        _theme1Id = themes[1].Id;

        var statuses = db.IdeaStatuses.ToDictionary(s => s.Code, s => s.Id);

        var now = DateTime.UtcNow;
        var oneMonthAgo = now.AddMonths(-1);
        var twoMonthsAgo = now.AddMonths(-2);

        Idea MakeIdea(string code, string statusCode, int currentStage, Guid submitterId, Guid themeId, DateTime createdAt, int? updatedAtOffsetDays = null)
        {
            var idea = new Idea
            {
                Id = Guid.NewGuid(),
                Code = code,
                TitleAr = "ا", TitleEn = "T", ProblemStatementAr = "م", ProblemStatementEn = "P",
                ProposedSolutionAr = "ح", ProposedSolutionEn = "S", ExpectedBenefitsAr = "ف", ExpectedBenefitsEn = "B",
                StrategicThemeId = themeId,
                IdeaStatusId = statuses[statusCode],
                SubmitterId = submitterId,
                CurrentStage = currentStage,
                CreatedAt = createdAt,
                UpdatedAt = updatedAtOffsetDays.HasValue ? createdAt.AddDays(updatedAtOffsetDays.Value) : createdAt,
            };
            db.Ideas.Add(idea);
            return idea;
        }

        // 7 ideas assigned to theme0's submitter1, 3 to theme1's submitter2 (2 active submitters).
        // CurrentStage reflects what production actually writes: 0/1 for every pre-pilot idea
        // (screening/evaluation/committee/approval never advance the stored column — see
        // IdeaJourneyCalculator, the authoritative on-demand stage source) and 6/7/8 only once
        // an idea has genuinely entered the post-program pilot/measurement/scaling statuses.
        var ideaA = MakeIdea("EXEC-A", IdeaStatusCodes.Draft, 0, submitter1Id, _theme0Id, now);
        var ideaB = MakeIdea("EXEC-B", IdeaStatusCodes.Submitted, 1, submitter1Id, _theme0Id, now, 2);
        var ideaC = MakeIdea("EXEC-C", IdeaStatusCodes.Evaluation, 1, submitter1Id, _theme0Id, oneMonthAgo, 4);
        var ideaD = MakeIdea("EXEC-D", IdeaStatusCodes.Approved, 1, submitter1Id, _theme0Id, twoMonthsAgo, 10);
        var ideaE = MakeIdea("EXEC-E", IdeaStatusCodes.InPilot, 6, submitter1Id, _theme0Id, twoMonthsAgo, 12);
        var ideaF = MakeIdea("EXEC-F", IdeaStatusCodes.InMeasurement, 7, submitter2Id, _theme0Id, now, 14);
        var ideaG = MakeIdea("EXEC-G", IdeaStatusCodes.InScaling, 8, submitter2Id, _theme0Id, now, 16);
        var ideaH = MakeIdea("EXEC-H", IdeaStatusCodes.Rejected, 1, submitter2Id, _theme1Id, now, 8);
        var ideaI = MakeIdea("EXEC-I", IdeaStatusCodes.EvaluationFailed, 1, submitter2Id, _theme1Id, now, 4);
        var ideaJ = MakeIdea("EXEC-J", IdeaStatusCodes.NotSelected, 1, submitter2Id, _theme1Id, now, 10);
        db.SaveChanges();

        // Evaluations (TotalEvaluations = 3).
        db.Evaluations.AddRange(
            new Evaluation { Id = Guid.NewGuid(), IdeaId = ideaC.Id, EvaluatorId = evaluatorId, TotalScore = 7 },
            new Evaluation { Id = Guid.NewGuid(), IdeaId = ideaD.Id, EvaluatorId = evaluatorId, TotalScore = 8 },
            new Evaluation { Id = Guid.NewGuid(), IdeaId = ideaE.Id, EvaluatorId = evaluatorId, TotalScore = 9 });
        db.SaveChanges();

        // Financial + non-financial benefits (only financial-category RealizedValue should be summed).
        var financialCategoryId = db.BenefitCategories.Single(c => c.Code == "financial").Id;
        var operationalCategoryId = db.BenefitCategories.Single(c => c.Code == "operational").Id;
        var quantitativeTypeId = db.BenefitTypes.Single(t => t.Code == "quantitative").Id;

        db.Benefits.AddRange(
            new Benefit
            {
                Id = Guid.NewGuid(),
                IdeaId = ideaD.Id,
                TitleAr = "أ", TitleEn = "Financial Benefit 1",
                BenefitTypeId = quantitativeTypeId,
                BenefitCategoryId = financialCategoryId,
                TargetValue = 20000m,
                RealizedValue = 15000.50m,
            },
            new Benefit
            {
                Id = Guid.NewGuid(),
                IdeaId = ideaE.Id,
                TitleAr = "ب", TitleEn = "Financial Benefit 2",
                BenefitTypeId = quantitativeTypeId,
                BenefitCategoryId = financialCategoryId,
                TargetValue = 1000m,
                RealizedValue = 500.25m,
            },
            new Benefit
            {
                Id = Guid.NewGuid(),
                IdeaId = ideaF.Id,
                TitleAr = "ج", TitleEn = "Operational Benefit",
                BenefitTypeId = quantitativeTypeId,
                BenefitCategoryId = operationalCategoryId,
                TargetValue = 999m,
                RealizedValue = 999m,
            });
        db.SaveChanges();
    }

    [Fact]
    public async Task Get_AsSupervisor_ReturnsAllSections()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.GetAsync("/api/analytics/executive");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(body.TryGetProperty("kpis", out _));
        Assert.True(body.TryGetProperty("funnel", out _));
        Assert.True(body.TryGetProperty("cohort", out _));
        Assert.True(body.TryGetProperty("ideasByStage", out _));
        Assert.True(body.TryGetProperty("submissions", out _));
        Assert.True(body.TryGetProperty("topObjectives", out _));
        Assert.True(body.TryGetProperty("avgTimePerStage", out _));
        Assert.True(body.TryGetProperty("conversion", out _));
    }

    [Fact]
    public async Task Get_AsJudge_Succeeds()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "judge1");

        var response = await client.GetAsync("/api/analytics/executive");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_AsSubmitter_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.GetAsync("/api/analytics/executive");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Funnel_HasFiveEntriesWithCorrectCounts()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var body = await client.GetFromJsonAsync<JsonElement>("/api/analytics/executive");
        var funnel = body.GetProperty("funnel").EnumerateArray().ToList();

        Assert.Equal(5, funnel.Count);

        int CountFor(string key) => funnel.Single(f => string.Equals(f.GetProperty("stageKey").GetString(), key, StringComparison.OrdinalIgnoreCase)).GetProperty("count").GetInt32();

        Assert.Equal(9, CountFor("Participation")); // non-draft: all but ideaA
        Assert.Equal(5, CountFor("Evaluated"));      // status reached evaluation-or-beyond: C,D,E,F,G (H/I/J stopped before/at rejection, not counted)
        Assert.Equal(1, CountFor("Approved"));       // status approved: D
        Assert.Equal(3, CountFor("Piloted"));        // status in_pilot/in_measurement/in_scaling: E,F,G
        Assert.Equal(1, CountFor("Scaled"));         // status in_scaling only: G (F is in_measurement, not scaled)
    }

    [Fact]
    public async Task Cohort_AggregatesAcrossTwelveMonths()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var body = await client.GetFromJsonAsync<JsonElement>("/api/analytics/executive");
        var cohort = body.GetProperty("cohort").EnumerateArray().ToList();

        Assert.Equal(12, cohort.Count);

        var totalSubmitted = cohort.Sum(c => c.GetProperty("submitted").GetInt32());
        var totalApproved = cohort.Sum(c => c.GetProperty("approved").GetInt32());
        var totalRejected = cohort.Sum(c => c.GetProperty("rejected").GetInt32());
        var totalImplemented = cohort.Sum(c => c.GetProperty("implemented").GetInt32());

        Assert.Equal(10, totalSubmitted);
        Assert.Equal(4, totalApproved);     // D(approved),E(in_pilot),F(in_measurement),G(in_scaling)
        Assert.Equal(3, totalRejected);     // H(rejected),I(evaluation_failed),J(not_selected)
        Assert.Equal(2, totalImplemented);  // F,G
    }

    [Fact]
    public async Task IdeasByStage_HasNineBucketsZeroToEight()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var body = await client.GetFromJsonAsync<JsonElement>("/api/analytics/executive");
        var byStage = body.GetProperty("ideasByStage").EnumerateArray().ToList();

        Assert.Equal(9, byStage.Count);
        int CountAt(int stage) => byStage.Single(e => e.GetProperty("stage").GetInt32() == stage).GetProperty("count").GetInt32();

        // Effective stage is derived from status (falling back to CurrentStage only once it's
        // genuinely >= 6): A=draft->0, B=submitted->1, C=evaluation->2, I=evaluation_failed->2,
        // H=rejected->4, D=approved->5, J=not_selected->5, E=in_pilot->6, F=in_measurement->7,
        // G=in_scaling->8. Stage 3 (pass_awaiting_attachments) is unused by this dataset.
        var expected = new Dictionary<int, int> { [0] = 1, [1] = 1, [2] = 2, [3] = 0, [4] = 1, [5] = 2, [6] = 1, [7] = 1, [8] = 1 };
        foreach (var (stage, count) in expected)
        {
            Assert.Equal(count, CountAt(stage));
        }
    }

    [Fact]
    public async Task Submissions_AreZeroFilledForNinetyDays()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var body = await client.GetFromJsonAsync<JsonElement>("/api/analytics/executive");
        var submissions = body.GetProperty("submissions").EnumerateArray().ToList();

        Assert.Equal(90, submissions.Count);
        Assert.Equal(9, submissions.Sum(s => s.GetProperty("count").GetInt32())); // excludes draft ideaA
    }

    [Fact]
    public async Task TopObjectives_OrderedDescendingByCount()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var body = await client.GetFromJsonAsync<JsonElement>("/api/analytics/executive");
        var topObjectives = body.GetProperty("topObjectives").EnumerateArray().ToList();

        Assert.Equal(2, topObjectives.Count);
        Assert.Equal(_theme0Id, topObjectives[0].GetProperty("themeId").GetGuid());
        Assert.Equal(7, topObjectives[0].GetProperty("count").GetInt32());
        Assert.Equal(_theme1Id, topObjectives[1].GetProperty("themeId").GetGuid());
        Assert.Equal(3, topObjectives[1].GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task AvgTimePerStage_HasEightBucketsEachAveraging2Days()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var body = await client.GetFromJsonAsync<JsonElement>("/api/analytics/executive");
        var avgTimePerStage = body.GetProperty("avgTimePerStage").EnumerateArray().ToList();

        Assert.Equal(8, avgTimePerStage.Count);
        foreach (var entry in avgTimePerStage)
        {
            Assert.Equal(2.0, entry.GetProperty("avgDays").GetDouble(), 3);
        }
    }

    [Fact]
    public async Task Conversion_ComputesRate()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var body = await client.GetFromJsonAsync<JsonElement>("/api/analytics/executive");
        var conversion = body.GetProperty("conversion");

        Assert.Equal(9, conversion.GetProperty("submitted").GetInt32());
        Assert.Equal(3, conversion.GetProperty("pilot").GetInt32());
        Assert.Equal(33.3, conversion.GetProperty("rate").GetDouble(), 1);
    }

    [Fact]
    public async Task ExtendedKpis_ComputesAllEightFields()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var body = await client.GetFromJsonAsync<JsonElement>("/api/analytics/executive");
        var kpis = body.GetProperty("kpis");

        Assert.Equal(9, kpis.GetProperty("totalSubmissions").GetInt32());
        Assert.Equal(1, kpis.GetProperty("totalApproved").GetInt32());
        Assert.Equal(2, kpis.GetProperty("totalImplemented").GetInt32());
        Assert.Equal(2, kpis.GetProperty("activeSubmitters").GetInt32());
        Assert.Equal(3, kpis.GetProperty("totalEvaluations").GetInt32());
        // 6 seeded test users + 1 seed-data "system" user (owner of the strategic themes).
        Assert.Equal(7, kpis.GetProperty("totalUsers").GetInt32());
        Assert.Equal(1, kpis.GetProperty("totalEvaluators").GetInt32());
        Assert.Equal(15500.75m, kpis.GetProperty("realizedFinancialImpact").GetDecimal());
    }

    public void Dispose()
    {
        _connection.Dispose();
        if (Directory.Exists(_rootPath)) Directory.Delete(_rootPath, recursive: true);
    }
}
