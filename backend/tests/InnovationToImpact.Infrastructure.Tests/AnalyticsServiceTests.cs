using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Analytics;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class AnalyticsServiceTests
{
    private static Guid SeedUser(SqliteContextFixture fixture, string samAccountName, string? roleCode = null)
    {
        using var db = fixture.CreateContext();
        var id = Guid.NewGuid();
        db.Users.Add(new User { Id = id, SamAccountName = samAccountName, Email = $"{samAccountName}@gac-demo.sa", FullNameAr = samAccountName, FullNameEn = samAccountName });
        db.SaveChanges();
        if (roleCode is not null)
        {
            var roleId = db.Roles.Single(r => r.Code == roleCode).Id;
            db.Set<UserRole>().Add(new UserRole { UserId = id, RoleId = roleId, IsPrimary = true });
            db.SaveChanges();
        }
        return id;
    }

    private static Guid SeedIdea(SqliteContextFixture fixture, Guid submitterId, Guid themeId, string statusCode)
    {
        using var db = fixture.CreateContext();
        var statusId = db.IdeaStatuses.Single(s => s.Code == statusCode).Id;
        var idea = new Idea
        {
            Id = Guid.NewGuid(),
            Code = $"A{Guid.NewGuid():N}"[..10],
            TitleAr = "ا",
            TitleEn = "T",
            ProblemStatementAr = "م",
            ProblemStatementEn = "P",
            ProposedSolutionAr = "ح",
            ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ف",
            ExpectedBenefitsEn = "B",
            StrategicThemeId = themeId,
            IdeaStatusId = statusId,
            SubmitterId = submitterId,
        };
        db.Ideas.Add(idea);
        db.SaveChanges();
        return idea.Id;
    }

    [Fact]
    public async Task GetPlatformKpisAsync_ComputesAllFiveCounts()
    {
        using var fixture = new SqliteContextFixture();
        var submitter1 = SeedUser(fixture, "submitter1");
        var submitter2 = SeedUser(fixture, "submitter2");
        SeedUser(fixture, "evaluator1", "evaluator");
        SeedUser(fixture, "evaluator2", "evaluator");
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;

        SeedIdea(fixture, submitter1, themeId, "approved");
        SeedIdea(fixture, submitter1, themeId, "draft");
        SeedIdea(fixture, submitter2, themeId, "approved");

        using var db = fixture.CreateContext();
        var service = new AnalyticsService(db);

        var kpis = await service.GetPlatformKpisAsync();

        Assert.Equal(3, kpis.TotalIdeas);
        Assert.Equal(2, kpis.TotalApproved);
        Assert.Equal(2, kpis.TotalSubmitters);
        Assert.Equal(0, kpis.TotalEvaluations);
        Assert.Equal(2, kpis.TotalEvaluators);
    }

    [Fact]
    public async Task GetPlatformKpisAsync_NoData_ReturnsAllZeros()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new AnalyticsService(db);

        var kpis = await service.GetPlatformKpisAsync();

        Assert.Equal(0, kpis.TotalIdeas);
        Assert.Equal(0, kpis.TotalApproved);
        Assert.Equal(0, kpis.TotalSubmitters);
        Assert.Equal(0, kpis.TotalEvaluations);
        Assert.Equal(0, kpis.TotalEvaluators);
    }

    [Fact]
    public async Task GetIdeasByStatusAsync_ReturnsElevenReachableCodesZeroFilled()
    {
        using var fixture = new SqliteContextFixture();
        var submitter = SeedUser(fixture, "submitter3");
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        SeedIdea(fixture, submitter, themeId, "draft");
        SeedIdea(fixture, submitter, themeId, "draft");
        SeedIdea(fixture, submitter, themeId, "approved");

        using var db = fixture.CreateContext();
        var service = new AnalyticsService(db);

        var result = await service.GetIdeasByStatusAsync();

        Assert.Equal(11, result.Count);
        Assert.Equal(2, result.Single(r => r.StatusCode == "draft").Count);
        Assert.Equal(1, result.Single(r => r.StatusCode == "approved").Count);
        Assert.Equal(0, result.Single(r => r.StatusCode == "submitted").Count);
        Assert.DoesNotContain(result, r => r.StatusCode == "screening");
    }

    [Fact]
    public async Task GetIdeasByStatusAsync_NoIdeas_AllElevenAtZero()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new AnalyticsService(db);

        var result = await service.GetIdeasByStatusAsync();

        Assert.Equal(11, result.Count);
        Assert.All(result, r => Assert.Equal(0, r.Count));
    }

    [Fact]
    public async Task GetSubmissionsOverTimeAsync_GroupsByCreatedDate_WithinLast90Days()
    {
        using var fixture = new SqliteContextFixture();
        var submitter = SeedUser(fixture, "submitter4");
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;

        var recentId1 = SeedIdea(fixture, submitter, themeId, "draft");
        var recentId2 = SeedIdea(fixture, submitter, themeId, "draft");
        var oldId = SeedIdea(fixture, submitter, themeId, "draft");

        using (var db = fixture.CreateContext())
        {
            var recent = db.Ideas.Single(i => i.Id == recentId1);
            recent.CreatedAt = DateTime.UtcNow.AddDays(-5);
            var recent2 = db.Ideas.Single(i => i.Id == recentId2);
            recent2.CreatedAt = DateTime.UtcNow.AddDays(-5);
            var old = db.Ideas.Single(i => i.Id == oldId);
            old.CreatedAt = DateTime.UtcNow.AddDays(-200);
            db.SaveChanges();
        }

        using var verifyDb = fixture.CreateContext();
        var service = new AnalyticsService(verifyDb);

        var result = await service.GetSubmissionsOverTimeAsync();

        var entry = Assert.Single(result);
        Assert.Equal(2, entry.Count);
        Assert.Equal(DateTime.UtcNow.AddDays(-5).Date, entry.Date);
    }

    [Fact]
    public async Task GetSubmissionsOverTimeAsync_NoRecentIdeas_ReturnsEmpty()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new AnalyticsService(db);

        var result = await service.GetSubmissionsOverTimeAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetThemeActivityAsync_ComputesIdeaAndApprovedCountsPerTheme()
    {
        using var fixture = new SqliteContextFixture();
        var submitter = SeedUser(fixture, "submitter5");
        using var themeDb = fixture.CreateContext();
        var themes = themeDb.StrategicThemes.ToList();
        var themeA = themes[0];
        var themeB = themes.Count > 1 ? themes[1] : themes[0];

        SeedIdea(fixture, submitter, themeA.Id, "approved");
        SeedIdea(fixture, submitter, themeA.Id, "draft");

        using var db = fixture.CreateContext();
        var service = new AnalyticsService(db);

        var result = await service.GetThemeActivityAsync();

        var entryA = result.Single(r => r.ThemeNameEn == themeA.NameEn);
        Assert.Equal(2, entryA.IdeaCount);
        Assert.Equal(1, entryA.ApprovedCount);
    }

    [Fact]
    public async Task GetThemeActivityAsync_ThemeWithNoIdeas_AppearsAtZero()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new AnalyticsService(db);

        var result = await service.GetThemeActivityAsync();

        Assert.NotEmpty(result);
        Assert.All(result, r => Assert.Equal(0, r.IdeaCount));
    }

    private static Guid SeedEvaluation(SqliteContextFixture fixture, Guid ideaId, Guid evaluatorId, decimal totalScore)
    {
        using var db = fixture.CreateContext();
        var evaluation = new Evaluation
        {
            Id = Guid.NewGuid(),
            IdeaId = ideaId,
            EvaluatorId = evaluatorId,
            TotalScore = totalScore,
            SubmittedAt = DateTime.UtcNow,
        };
        db.Evaluations.Add(evaluation);
        db.SaveChanges();
        return evaluation.Id;
    }

    private static Guid SeedSlaTracking(SqliteContextFixture fixture, string entityType, DateTime? breachedAt, DateTime? resolvedAt = null)
    {
        using var db = fixture.CreateContext();
        var policyId = db.SlaPolicies.Single(p => p.EntityType == entityType).Id;
        var tracking = new SlaTracking
        {
            Id = Guid.NewGuid(),
            SlaPolicyId = policyId,
            EntityId = Guid.NewGuid(),
            OpenedAt = DateTime.UtcNow.AddDays(-10),
            TargetAt = DateTime.UtcNow.AddDays(-1),
            BreachedAt = breachedAt,
            ResolvedAt = resolvedAt,
        };
        db.SlaTrackings.Add(tracking);
        db.SaveChanges();
        return tracking.Id;
    }

    [Fact]
    public async Task GetTopEvaluatorsAsync_ComputesCountAndAverageScorePerEvaluator_OrderedByCount()
    {
        using var fixture = new SqliteContextFixture();
        var submitter = SeedUser(fixture, "submitter6");
        var evaluator1 = SeedUser(fixture, "evaluator3", "evaluator");
        var evaluator2 = SeedUser(fixture, "evaluator4", "evaluator");
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea1 = SeedIdea(fixture, submitter, themeId, "evaluation");
        var idea2 = SeedIdea(fixture, submitter, themeId, "evaluation");
        var idea3 = SeedIdea(fixture, submitter, themeId, "evaluation");

        SeedEvaluation(fixture, idea1, evaluator1, 8m);
        SeedEvaluation(fixture, idea2, evaluator1, 6m);
        SeedEvaluation(fixture, idea3, evaluator2, 9m);

        using var db = fixture.CreateContext();
        var service = new AnalyticsService(db);

        var result = await service.GetTopEvaluatorsAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("evaluator3", result[0].EvaluatorNameEn);
        Assert.Equal(2, result[0].EvaluationCount);
        Assert.Equal(7m, result[0].AverageScore);
        Assert.Equal("evaluator4", result[1].EvaluatorNameEn);
        Assert.Equal(1, result[1].EvaluationCount);
    }

    [Fact]
    public async Task GetTopEvaluatorsAsync_NoEvaluations_ReturnsEmpty()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new AnalyticsService(db);

        var result = await service.GetTopEvaluatorsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSlaComplianceAsync_ComputesPercentageNotBreached()
    {
        using var fixture = new SqliteContextFixture();
        SeedSlaTracking(fixture, "evaluation", breachedAt: null);
        SeedSlaTracking(fixture, "evaluation", breachedAt: null);
        SeedSlaTracking(fixture, "evaluation", breachedAt: DateTime.UtcNow.AddDays(-1));
        SeedSlaTracking(fixture, "committee", breachedAt: null);

        using var db = fixture.CreateContext();
        var service = new AnalyticsService(db);

        var result = await service.GetSlaComplianceAsync();

        Assert.Equal(4, result.TotalTracked);
        Assert.Equal(75.0, result.CompliancePct);
    }

    [Fact]
    public async Task GetSlaComplianceAsync_NoTrackingRows_ReturnsNullPercentage()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new AnalyticsService(db);

        var result = await service.GetSlaComplianceAsync();

        Assert.Null(result.CompliancePct);
        Assert.Equal(0, result.TotalTracked);
    }
}
