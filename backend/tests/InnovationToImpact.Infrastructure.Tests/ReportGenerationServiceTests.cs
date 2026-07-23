using ClosedXML.Excel;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Reports;
using InnovationToImpact.Infrastructure.Analytics;
using InnovationToImpact.Infrastructure.Data;
using InnovationToImpact.Infrastructure.Reports;
using InnovationToImpact.Infrastructure.Reports.Bundle;
using InnovationToImpact.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class ReportGenerationServiceTests : IDisposable
{
    static ReportGenerationServiceTests()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    }

    private readonly SqliteContextFixture _fixture = new();
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"report-storage-test-{Guid.NewGuid():N}");

    private static Guid SeedUser(InnovationDbContext db, string samAccountName)
    {
        var userId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, SamAccountName = samAccountName, Email = $"{samAccountName}@gac-demo.sa", FullNameAr = samAccountName, FullNameEn = samAccountName });
        db.SaveChanges();
        return userId;
    }

    private ReportGenerationService BuildService(InnovationDbContext db)
    {
        var storage = new LocalDiskFileStorage(_rootPath);
        return new ReportGenerationService(
            db,
            new AuditLogReportGenerator(db),
            new IdeasReportGenerator(db),
            new EvaluationsReportGenerator(db),
            new EscalationsReportGenerator(db),
            new AnalyticsReportGenerator(new AnalyticsService(db)),
            storage,
            new ReportBundleBuilder(db),
            new ReportBundleXlsxRenderer(),
            new ReportBundlePdfRenderer(),
            new ReportBundlePptxRenderer());
    }

    [Fact]
    public async Task GeneratesAuditLogReport_UpdatesStatusToCompleted_WritesRealXlsxFile()
    {
        using var db = _fixture.CreateContext();
        var userId = SeedUser(db, "requester1");
        db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ChainSeq = 1, RowHash = new string('a', 64), EntityType = "idea", EntityId = Guid.NewGuid(), Action = "create", OccurredAt = DateTime.UtcNow });
        db.SaveChanges();

        var service = BuildService(db);

        var result = await service.GenerateAuditLogReportAsync(userId, CancellationToken.None);

        Assert.Equal(ReportGenerationStatusCodes.Completed, result.Status);
        Assert.NotNull(result.FileUrl);
        Assert.True(File.Exists(result.FileUrl));

        var stored = await db.ReportGenerations.SingleAsync(g => g.Id == result.ReportGenerationId);
        Assert.Equal(ReportGenerationStatusCodes.Completed, stored.Status);
        Assert.Equal(ReportFormatCodes.Xlsx, stored.Format);
        Assert.Equal(userId, stored.RequestedById);
        Assert.NotNull(stored.CompletedAt);

        using var workbook = new XLWorkbook(result.FileUrl!);
        var worksheet = workbook.Worksheet("Audit Log");
        Assert.Equal(2, worksheet.LastRowUsed()!.RowNumber());
    }

    [Fact]
    public async Task GeneratesIdeasReport_UpdatesStatusToCompleted_LinksToIdeasExportReportTitle()
    {
        using var db = _fixture.CreateContext();
        var submitterId = SeedUser(db, "requester2");
        var themeId = db.StrategicThemes.First().Id;
        var statusId = db.IdeaStatuses.Single(s => s.Code == "draft").Id;
        db.Ideas.Add(new Idea
        {
            Id = Guid.NewGuid(), Code = "IDEA-SVC-1", TitleAr = "ا", TitleEn = "T",
            ProblemStatementAr = "م", ProblemStatementEn = "P", ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ف", ExpectedBenefitsEn = "B", StrategicThemeId = themeId,
            IdeaStatusId = statusId, SubmitterId = submitterId,
        });
        db.SaveChanges();

        var service = BuildService(db);

        var result = await service.GenerateIdeasReportAsync(submitterId, CancellationToken.None);

        Assert.Equal(ReportGenerationStatusCodes.Completed, result.Status);
        var stored = await db.ReportGenerations.Include(g => g.ReportTitle).SingleAsync(g => g.Id == result.ReportGenerationId);
        Assert.Equal("ideas_export", stored.ReportTitle.Key);
    }

    [Fact]
    public async Task GeneratesEvaluationsReport_UpdatesStatusToCompleted_LinksToEvaluationsExportReportTitle()
    {
        using var db = _fixture.CreateContext();
        var userId = SeedUser(db, "requester3");
        var service = BuildService(db);

        var result = await service.GenerateEvaluationsReportAsync(userId, CancellationToken.None);

        Assert.Equal(ReportGenerationStatusCodes.Completed, result.Status);
        var stored = await db.ReportGenerations.Include(g => g.ReportTitle).SingleAsync(g => g.Id == result.ReportGenerationId);
        Assert.Equal("evaluations_export", stored.ReportTitle.Key);
    }

    [Fact]
    public async Task GeneratesEscalationsReport_UpdatesStatusToCompleted_LinksToEscalationsExportReportTitle()
    {
        using var db = _fixture.CreateContext();
        var userId = SeedUser(db, "requester4");
        var service = BuildService(db);

        var result = await service.GenerateEscalationsReportAsync(userId, CancellationToken.None);

        Assert.Equal(ReportGenerationStatusCodes.Completed, result.Status);
        var stored = await db.ReportGenerations.Include(g => g.ReportTitle).SingleAsync(g => g.Id == result.ReportGenerationId);
        Assert.Equal("escalations_export", stored.ReportTitle.Key);
    }

    [Fact]
    public async Task GeneratesAnalyticsReport_UpdatesStatusToCompleted_LinksToAnalyticsExportReportTitle_WritesMultiSheetXlsx()
    {
        using var db = _fixture.CreateContext();
        var userId = SeedUser(db, "requester5");
        var service = BuildService(db);

        var result = await service.GenerateAnalyticsReportAsync(userId, ReportFormatCodes.Xlsx, CancellationToken.None);

        Assert.Equal(ReportGenerationStatusCodes.Completed, result.Status);
        Assert.NotNull(result.FileUrl);
        var stored = await db.ReportGenerations.Include(g => g.ReportTitle).SingleAsync(g => g.Id == result.ReportGenerationId);
        Assert.Equal("analytics_export", stored.ReportTitle.Key);
        Assert.Equal(ReportFormatCodes.Xlsx, stored.Format);

        using var workbook = new XLWorkbook(result.FileUrl!);
        Assert.Equal(
            new[] { "KPIs", "Funnel", "Cohort", "IdeasByStage", "TopObjectives", "AvgTimePerStage", "Conversion" },
            workbook.Worksheets.Select(w => w.Name));
    }

    [Fact]
    public async Task GeneratesAnalyticsReport_UnsupportedFormat_MarksFailed()
    {
        using var db = _fixture.CreateContext();
        var userId = SeedUser(db, "requester6");
        var service = BuildService(db);

        // ReportFormatCodes.Pdf IS a supported format (AnalyticsReportGenerator handles it) —
        // using it here previously only "failed" because this test host never sets the QuestPDF
        // community license, which throws. That's not what this test claims to verify. Use a
        // genuinely unsupported format string so this test actually exercises the unsupported-
        // format failure path (AnalyticsReportGenerator.GenerateAsync's default `_ => throw`).
        var result = await service.GenerateAnalyticsReportAsync(userId, "bogus-format", CancellationToken.None);

        Assert.Equal(ReportGenerationStatusCodes.Failed, result.Status);
        Assert.Null(result.FileUrl);
    }

    public void Dispose()
    {
        _fixture.Dispose();
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
