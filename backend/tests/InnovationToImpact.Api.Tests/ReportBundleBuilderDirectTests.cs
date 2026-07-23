using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Domain.Reports.Bundle;
using InnovationToImpact.Infrastructure.Data;
using InnovationToImpact.Infrastructure.Reports.Bundle;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Api.Tests;

/// <summary>
/// Covers the five direct-read bundle builders (audit, operational, evaluators,
/// committee, cx) — the ones that read AuditLogs/SlaTrackings/Evaluations/
/// CommitteeDecisions/SupportMessages rather than the Ideas graph.
/// </summary>
public class ReportBundleBuilderDirectTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly InnovationDbContext _db;

    public ReportBundleBuilderDirectTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<InnovationDbContext>().UseSqlite(_connection).Options;
        _db = new InnovationDbContext(options);
        _db.Database.EnsureCreated();

        var owner = new User
        {
            Id = Guid.NewGuid(),
            SamAccountName = "owner2",
            Email = "owner2@gac-demo.sa",
            FullNameAr = "المالك الثاني",
            FullNameEn = "Owner Two",
        };
        var submitter = new User
        {
            Id = Guid.NewGuid(),
            SamAccountName = "innovator2",
            Email = "innovator2@gac-demo.sa",
            FullNameAr = "المبتكر الثاني",
            FullNameEn = "Innovator Two",
        };
        var evaluator = new User
        {
            Id = Guid.NewGuid(),
            SamAccountName = "evaluator1",
            Email = "evaluator1@gac-demo.sa",
            FullNameAr = "المقيم الأول",
            FullNameEn = "Evaluator One",
        };
        _db.Users.AddRange(owner, submitter, evaluator);

        var theme = new StrategicTheme
        {
            Id = Guid.NewGuid(),
            NameAr = "مسار تجريبي",
            NameEn = "Test Theme",
            Priority = 1,
            OwnerId = owner.Id,
        };
        _db.StrategicThemes.Add(theme);

        _db.SaveChanges();

        var submittedStatus = _db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Submitted);
        var returnedStatus = _db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Returned);

        var createdAt = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        var queueIdea = NewIdea("IDEA-Q01", "Queued Idea", submittedStatus.Id, theme.Id, submitter.Id, 1, createdAt);
        var returnedIdea = NewIdea("IDEA-R01", "Returned Idea", returnedStatus.Id, theme.Id, submitter.Id, 1, createdAt);
        _db.Ideas.AddRange(queueIdea, returnedIdea);

        _db.SaveChanges();

        // Audit log — one row, newest (and only) entry.
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ChainSeq = 1,
            RowHash = "hash1",
            EntityType = "Idea",
            EntityId = queueIdea.Id,
            Action = "create",
            ActorId = submitter.Id,
            OccurredAt = new DateTime(2026, 6, 2, 10, 0, 0, DateTimeKind.Utc),
        });

        // SLA policy + tracking row (breached).
        var slaPolicy = new SlaPolicy
        {
            Id = Guid.NewGuid(),
            EntityType = "Idea",
            FromState = "submitted",
            ToState = "evaluation",
            TargetHours = 48,
            WarnAtPct = 80,
        };
        _db.SlaPolicies.Add(slaPolicy);
        _db.SaveChanges();

        _db.SlaTrackings.Add(new SlaTracking
        {
            Id = Guid.NewGuid(),
            SlaPolicyId = slaPolicy.Id,
            EntityId = queueIdea.Id,
            OpenedAt = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            TargetAt = new DateTime(2026, 6, 3, 0, 0, 0, DateTimeKind.Utc),
            BreachedAt = new DateTime(2026, 6, 3, 5, 0, 0, DateTimeKind.Utc),
        });

        // Two evaluations by one evaluator (on different ideas — Evaluations has a
        // unique constraint on (IdeaId, EvaluatorId)), one submitted.
        _db.Evaluations.AddRange(
            new Evaluation
            {
                Id = Guid.NewGuid(),
                IdeaId = queueIdea.Id,
                EvaluatorId = evaluator.Id,
                CriteriaScoresJson = "{}",
                TotalScore = 80m,
                SubmittedAt = new DateTime(2026, 6, 4, 0, 0, 0, DateTimeKind.Utc),
            },
            new Evaluation
            {
                Id = Guid.NewGuid(),
                IdeaId = returnedIdea.Id,
                EvaluatorId = evaluator.Id,
                CriteriaScoresJson = "{}",
                TotalScore = 91m,
                SubmittedAt = null,
            });

        // Committee decision.
        var approvedType = _db.CommitteeDecisionTypes.Single(t => t.Code == "approved");
        _db.CommitteeDecisions.Add(new CommitteeDecision
        {
            Id = Guid.NewGuid(),
            IdeaId = queueIdea.Id,
            CommitteeName = "Innovation Committee",
            CommitteeDecisionTypeId = approvedType.Id,
            CriteriaScoresJson = "{}",
            TotalScore = 90m,
            QuorumMet = true,
            DecidedAt = new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc),
        });

        // Support message.
        _db.SupportMessages.Add(new SupportMessage
        {
            Id = Guid.NewGuid(),
            Name = "Jane Doe",
            Email = "jane@example.com",
            Subject = "Question about my idea",
            Body = "Please check the status of my submission.",
            Handled = false,
            CreatedAt = new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc),
        });

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
    public async Task Audit_bundle_lists_the_log_row()
    {
        var builder = new ReportBundleBuilder(_db);
        var bundle = await builder.BuildAsync(ReportTypeCodes.Audit, null, null, null, "tester");

        Assert.Equal(ReportTypeCodes.Audit, bundle.Type);
        Assert.Contains(bundle.Kpis, k => k.LabelEn == "Total Records" && k.Value == "1");

        var section = Assert.Single(bundle.Sections);
        var row = Assert.Single(section.Rows);
        Assert.Equal("2026-06-02", row["date"]);
        Assert.Equal("Idea", row["entity_type"]);
        Assert.Equal("create", row["action"]);
        Assert.False(string.IsNullOrEmpty(row["actor"]));
        Assert.False(string.IsNullOrEmpty(row["entity_id"]));
    }

    [Fact]
    public async Task Operational_bundle_counts_queue_and_sla_breaches()
    {
        var builder = new ReportBundleBuilder(_db);
        var bundle = await builder.BuildAsync(ReportTypeCodes.Operational, null, null, null, "tester");

        Assert.Contains(bundle.Kpis, k => k.LabelEn == "Ideas In Queue" && k.Value == "1");
        Assert.Contains(bundle.Kpis, k => k.LabelEn == "SLA Breaches" && k.Value == "1");
        Assert.Contains(bundle.Kpis, k => k.LabelEn == "SLA Records" && k.Value == "1");

        var section = Assert.Single(bundle.Sections);
        var row = Assert.Single(section.Rows);
        Assert.Equal("submitted→evaluation", row["sla_type"]);
        Assert.Equal("2026-06-03", row["due_at"]);
        Assert.Equal("breached", row["status"]);
        Assert.Equal("2026-06-03", row["breached_at"]);
    }

    [Fact]
    public async Task Evaluators_bundle_aggregates_per_evaluator()
    {
        var builder = new ReportBundleBuilder(_db);
        var bundle = await builder.BuildAsync(ReportTypeCodes.Evaluators, null, null, null, "tester");

        Assert.Contains(bundle.Kpis, k => k.LabelEn == "Total Evaluations" && k.Value == "2");
        Assert.Contains(bundle.Kpis, k => k.LabelEn == "Active Evaluators" && k.Value == "1");

        var section = Assert.Single(bundle.Sections);
        var row = Assert.Single(section.Rows);
        Assert.Equal("Evaluator One", row["evaluator"]);
        Assert.Equal("2", row["evaluations"]);
        Assert.Equal("1", row["submitted"]);
        Assert.Equal("85.5", row["avg_score"]);
    }

    [Fact]
    public async Task Evaluators_bundle_excludes_null_submitted_when_range_is_set()
    {
        // Seeded data has two evaluations for the same evaluator: one SubmittedAt =
        // 2026-06-04 (inside the range below) and one SubmittedAt = null. A null-submitted
        // evaluation has no date to place in the window, so once any bound is set it must
        // be excluded — Total Evaluations should reflect only the in-range row.
        var builder = new ReportBundleBuilder(_db);
        var from = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc);
        var bundle = await builder.BuildAsync(ReportTypeCodes.Evaluators, from, to, null, "tester");

        Assert.Contains(bundle.Kpis, k => k.LabelEn == "Total Evaluations" && k.Value == "1");
        Assert.Contains(bundle.Kpis, k => k.LabelEn == "Active Evaluators" && k.Value == "1");

        var section = Assert.Single(bundle.Sections);
        var row = Assert.Single(section.Rows);
        Assert.Equal("Evaluator One", row["evaluator"]);
        Assert.Equal("1", row["evaluations"]);
        Assert.Equal("1", row["submitted"]);
        Assert.Equal("80", row["avg_score"]);
    }

    [Fact]
    public async Task Cx_bundle_returned_kpi_respects_date_range()
    {
        // Seeded returnedIdea was created 2026-06-01 (inside the range below). Add a second
        // returned idea created well outside the range and assert the KPI counts only the
        // in-range one — snapshot KPIs must respect the report's date window.
        var submitter = _db.Users.Single(u => u.SamAccountName == "innovator2");
        var theme = _db.StrategicThemes.Single(t => t.NameEn == "Test Theme");
        var returnedStatus = _db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Returned);

        var outsideRangeIdea = NewIdea(
            "IDEA-R02",
            "Returned Idea Outside Range",
            returnedStatus.Id,
            theme.Id,
            submitter.Id,
            1,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        _db.Ideas.Add(outsideRangeIdea);
        _db.SaveChanges();

        var builder = new ReportBundleBuilder(_db);
        var from = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc);
        var bundle = await builder.BuildAsync(ReportTypeCodes.Cx, from, to, null, "tester");

        Assert.Contains(bundle.Kpis, k => k.LabelEn == "Ideas Returned to Innovator" && k.Value == "1");
    }

    [Fact]
    public async Task Committee_bundle_lists_the_decision()
    {
        var builder = new ReportBundleBuilder(_db);
        var bundle = await builder.BuildAsync(ReportTypeCodes.Committee, null, null, null, "tester");

        Assert.Contains(bundle.Kpis, k => k.LabelEn == "Total Decisions" && k.Value == "1");

        var section = Assert.Single(bundle.Sections);
        var row = Assert.Single(section.Rows);
        Assert.Equal("Innovation Committee", row["committee"]);
        Assert.Equal("approved", row["decision"]);
        Assert.Equal("Yes", row["quorum"]);
        Assert.Equal("2026-06-05", row["date"]);
    }

    [Fact]
    public async Task Cx_bundle_reports_returned_ideas_and_support_messages()
    {
        var builder = new ReportBundleBuilder(_db);
        var bundle = await builder.BuildAsync(ReportTypeCodes.Cx, null, null, null, "tester");

        Assert.Contains(bundle.Kpis, k => k.LabelEn == "Ideas Returned to Innovator" && k.Value == "1");
        Assert.Contains(bundle.Kpis, k => k.LabelEn == "Feedback Entries" && k.Value == "1");

        var section = Assert.Single(bundle.Sections);
        var row = Assert.Single(section.Rows);
        Assert.Equal("Jane Doe", row["name"]);
        Assert.Equal("Question about my idea", row["subject"]);
        Assert.Equal("Please check the status of my submission.", row["body"]);
        Assert.Equal("2026-06-06", row["date"]);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
