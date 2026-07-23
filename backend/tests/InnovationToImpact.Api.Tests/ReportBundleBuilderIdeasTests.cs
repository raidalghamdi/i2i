using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Domain.Reports.Bundle;
using InnovationToImpact.Infrastructure.Data;
using InnovationToImpact.Infrastructure.Reports.Bundle;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Api.Tests;

public class ReportBundleBuilderIdeasTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly InnovationDbContext _db;

    public ReportBundleBuilderIdeasTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<InnovationDbContext>().UseSqlite(_connection).Options;
        _db = new InnovationDbContext(options);
        _db.Database.EnsureCreated();

        var owner = new User
        {
            Id = Guid.NewGuid(),
            SamAccountName = "owner1",
            Email = "owner1@gac-demo.sa",
            FullNameAr = "المالك",
            FullNameEn = "Owner One",
        };
        var submitter = new User
        {
            Id = Guid.NewGuid(),
            SamAccountName = "innovator1",
            Email = "innovator1@gac-demo.sa",
            FullNameAr = "المبتكر الأول",
            FullNameEn = "Innovator One",
        };
        _db.Users.AddRange(owner, submitter);

        var theme = new StrategicTheme
        {
            Id = Guid.NewGuid(),
            NameAr = "الابتكار الرقمي",
            NameEn = "Digital Innovation",
            Priority = 1,
            OwnerId = owner.Id,
        };
        _db.StrategicThemes.Add(theme);

        _db.SaveChanges();

        var approvedStatus = _db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Approved);
        var inPilotStatus = _db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.InPilot);
        var rejectedStatus = _db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Rejected);
        var submittedStatus = _db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Submitted);

        var createdAt = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc);

        _db.Ideas.AddRange(
            NewIdea("IDEA-001", "Approved Idea", approvedStatus.Id, theme.Id, submitter.Id, 5, createdAt),
            NewIdea("IDEA-002", "In Pilot Idea", inPilotStatus.Id, theme.Id, submitter.Id, 6, createdAt),
            NewIdea("IDEA-003", "Rejected Idea", rejectedStatus.Id, theme.Id, submitter.Id, 2, createdAt),
            NewIdea("IDEA-004", "Submitted Idea", submittedStatus.Id, theme.Id, submitter.Id, 1, createdAt));

        _db.SaveChanges();
    }

    private static Idea NewIdea(string code, string title, Guid statusId, Guid themeId, Guid submitterId, int stage, DateTime createdAt) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        TitleAr = title,
        TitleEn = title,
        ProblemStatementAr = "problem",
        ProblemStatementEn = "problem",
        ProposedSolutionAr = "solution",
        ProposedSolutionEn = "solution",
        ExpectedBenefitsAr = "benefit",
        ExpectedBenefitsEn = "benefit",
        StrategicThemeId = themeId,
        IdeaStatusId = statusId,
        CurrentStage = stage,
        SubmitterId = submitterId,
        CreatedAt = createdAt,
        UpdatedAt = createdAt,
    };

    [Fact]
    public async Task Executive_bundle_has_totals_and_theme_distribution()
    {
        var builder = new ReportBundleBuilder(_db);
        var bundle = await builder.BuildAsync(ReportTypeCodes.Executive, null, null, null, "tester");

        Assert.Equal(ReportTypeCodes.Executive, bundle.Type);
        Assert.Contains(bundle.Kpis, k => k.LabelEn == "Total" && k.Value == "4");
        Assert.Contains(bundle.Kpis, k => k.LabelEn == "Approved" && k.Value == "2");
        Assert.Contains(bundle.Kpis, k => k.LabelEn == "Rejected" && k.Value == "1");
        Assert.Contains(bundle.Kpis, k => k.LabelEn == "In Progress" && k.Value == "1");
        Assert.Contains(bundle.Kpis, k => k.LabelEn == "Implemented" && k.Value == "1");
        Assert.Contains(bundle.Kpis, k => k.LabelEn == "Approval Rate" && k.Value == "50%");

        var section = Assert.Single(bundle.Sections);
        var row = Assert.Single(section.Rows);
        Assert.Equal("Digital Innovation", row["theme"]);
        Assert.Equal("4", row["count"]);
        Assert.Equal("100%", row["share"]);
    }

    [Fact]
    public async Task Detailed_and_ideas_bundles_list_one_row_per_idea_with_seven_columns()
    {
        var builder = new ReportBundleBuilder(_db);

        foreach (var type in new[] { ReportTypeCodes.Detailed, ReportTypeCodes.Ideas })
        {
            var bundle = await builder.BuildAsync(type, null, null, null, "tester");
            var section = Assert.Single(bundle.Sections);
            Assert.Equal(7, section.Columns.Count);
            Assert.Equal(4, section.Rows.Count);

            var row = section.Rows.Single(r => r["code"] == "IDEA-001");
            Assert.Equal("Approved Idea", row["title"]);
            Assert.Equal(IdeaStatusCodes.Approved, row["status"]);
            Assert.Equal("Digital Innovation", row["theme"]);
            Assert.Equal("Innovator One", row["submitter"]);
            Assert.Equal("5", row["stage"]);
            Assert.Equal("2026-05-15", row["created_at"]);
        }
    }

    [Fact]
    public async Task Media_bundle_includes_only_approved_or_beyond_ideas()
    {
        var builder = new ReportBundleBuilder(_db);
        var bundle = await builder.BuildAsync(ReportTypeCodes.Media, null, null, null, "tester");

        var section = Assert.Single(bundle.Sections);
        Assert.Equal(2, section.Rows.Count);
        Assert.All(section.Rows, r => Assert.Contains(r["stage"], new[] { IdeaStatusCodes.Approved, IdeaStatusCodes.InPilot }));

        var approvedRow = section.Rows.Single(r => r["title"] == "Approved Idea");
        Assert.Equal("benefit", approvedRow["expected_benefit"]);
        Assert.Equal("Digital Innovation", approvedRow["theme"]);
        Assert.Equal(IdeaStatusCodes.Approved, approvedRow["stage"]);
    }

    [Fact]
    public async Task Detailed_bundle_filters_by_theme_id()
    {
        var owner = _db.Users.Single(u => u.SamAccountName == "owner1");
        var submitter = _db.Users.Single(u => u.SamAccountName == "innovator1");
        var otherTheme = new StrategicTheme
        {
            Id = Guid.NewGuid(),
            NameAr = "مسار آخر",
            NameEn = "Other Theme",
            Priority = 2,
            OwnerId = owner.Id,
        };
        _db.StrategicThemes.Add(otherTheme);
        _db.SaveChanges();

        var approvedStatus = _db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Approved);
        var otherIdea = NewIdea(
            "IDEA-005",
            "Other Theme Idea",
            approvedStatus.Id,
            otherTheme.Id,
            submitter.Id,
            3,
            new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc));
        _db.Ideas.Add(otherIdea);
        _db.SaveChanges();

        var originalTheme = _db.StrategicThemes.Single(t => t.NameEn == "Digital Innovation");

        var builder = new ReportBundleBuilder(_db);
        var bundle = await builder.BuildAsync(ReportTypeCodes.Detailed, null, null, originalTheme.Id, "tester");

        var section = Assert.Single(bundle.Sections);
        Assert.Equal(4, section.Rows.Count);
        Assert.All(section.Rows, r => Assert.Equal("Digital Innovation", r["theme"]));
        Assert.DoesNotContain(section.Rows, r => r["code"] == "IDEA-005");
    }

    [Fact]
    public async Task Themes_bundle_aggregates_counts_per_theme()
    {
        var builder = new ReportBundleBuilder(_db);
        var bundle = await builder.BuildAsync(ReportTypeCodes.Themes, null, null, null, "tester");

        var section = Assert.Single(bundle.Sections);
        var row = Assert.Single(section.Rows);
        Assert.Equal("Digital Innovation", row["theme"]);
        Assert.Equal("4", row["count"]);
        Assert.Equal("2", row["approved"]);
        Assert.Equal("1", row["rejected"]);
        Assert.Equal("50%", row["approval_rate"]);
    }

    [Fact]
    public async Task Innovators_bundle_aggregates_by_submitter()
    {
        var builder = new ReportBundleBuilder(_db);
        var bundle = await builder.BuildAsync(ReportTypeCodes.Innovators, null, null, null, "tester");

        var section = Assert.Single(bundle.Sections);
        var row = Assert.Single(section.Rows);
        Assert.Equal("Innovator One", row["innovator"]);
        Assert.Equal("4", row["ideas"]);
        Assert.Equal("2", row["approved"]);
    }

    [Fact]
    public async Task Trends_bundle_has_a_monthly_row()
    {
        var builder = new ReportBundleBuilder(_db);
        var bundle = await builder.BuildAsync(ReportTypeCodes.Trends, null, null, null, "tester");

        var section = Assert.Single(bundle.Sections);
        var row = Assert.Single(section.Rows);
        Assert.Equal("2026-05", row["month"]);
        Assert.Equal("4", row["submitted"]);
        Assert.Equal("2", row["approved"]);
        Assert.Equal("50%", row["approval_rate"]);
    }

    [Fact]
    public async Task BuildAsync_throws_not_supported_for_an_unknown_type()
    {
        var builder = new ReportBundleBuilder(_db);
        await Assert.ThrowsAsync<NotSupportedException>(() => builder.BuildAsync("not-a-real-type", null, null, null, "tester"));
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
