using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Reports;
using InnovationToImpact.Domain.Reports.Bundle;
using InnovationToImpact.Infrastructure.Data;
using InnovationToImpact.Infrastructure.Reports.Bundle;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Reports;

public class ReportGenerationService : IReportGenerationService
{
    private readonly InnovationDbContext _db;
    private readonly IAuditLogReportGenerator _auditLogGenerator;
    private readonly IIdeasReportGenerator _ideasGenerator;
    private readonly IEvaluationsReportGenerator _evaluationsGenerator;
    private readonly IEscalationsReportGenerator _escalationsGenerator;
    private readonly IAnalyticsReportGenerator _analyticsGenerator;
    private readonly IReportFileStorage _storage;
    private readonly IReportBundleBuilder _builder;
    private readonly IReportBundleXlsxRenderer _renderer;
    private readonly IReportBundlePdfRenderer _pdfRenderer;
    private readonly IReportBundlePptxRenderer _pptxRenderer;

    public ReportGenerationService(
        InnovationDbContext db,
        IAuditLogReportGenerator auditLogGenerator,
        IIdeasReportGenerator ideasGenerator,
        IEvaluationsReportGenerator evaluationsGenerator,
        IEscalationsReportGenerator escalationsGenerator,
        IAnalyticsReportGenerator analyticsGenerator,
        IReportFileStorage storage,
        IReportBundleBuilder builder,
        IReportBundleXlsxRenderer renderer,
        IReportBundlePdfRenderer pdfRenderer,
        IReportBundlePptxRenderer pptxRenderer)
    {
        _db = db;
        _auditLogGenerator = auditLogGenerator;
        _ideasGenerator = ideasGenerator;
        _evaluationsGenerator = evaluationsGenerator;
        _escalationsGenerator = escalationsGenerator;
        _analyticsGenerator = analyticsGenerator;
        _storage = storage;
        _builder = builder;
        _renderer = renderer;
        _pdfRenderer = pdfRenderer;
        _pptxRenderer = pptxRenderer;
    }

    public Task<AuditLogReportResult> GenerateAuditLogReportAsync(Guid requestedByUserId, CancellationToken cancellationToken = default) =>
        GenerateReportAsync("audit_log_export", "audit-log.xlsx", ReportFormatCodes.Xlsx, _auditLogGenerator.GenerateAsync, requestedByUserId, cancellationToken);

    public Task<AuditLogReportResult> GenerateIdeasReportAsync(Guid requestedByUserId, CancellationToken cancellationToken = default) =>
        GenerateReportAsync("ideas_export", "ideas.xlsx", ReportFormatCodes.Xlsx, _ideasGenerator.GenerateAsync, requestedByUserId, cancellationToken);

    public Task<AuditLogReportResult> GenerateEvaluationsReportAsync(Guid requestedByUserId, CancellationToken cancellationToken = default) =>
        GenerateReportAsync("evaluations_export", "evaluations.xlsx", ReportFormatCodes.Xlsx, _evaluationsGenerator.GenerateAsync, requestedByUserId, cancellationToken);

    public Task<AuditLogReportResult> GenerateEscalationsReportAsync(Guid requestedByUserId, CancellationToken cancellationToken = default) =>
        GenerateReportAsync("escalations_export", "escalations.xlsx", ReportFormatCodes.Xlsx, _escalationsGenerator.GenerateAsync, requestedByUserId, cancellationToken);

    public Task<AuditLogReportResult> GenerateAnalyticsReportAsync(Guid requestedByUserId, string format, CancellationToken cancellationToken = default) =>
        GenerateReportAsync("analytics_export", $"analytics.{format}", format, ct => _analyticsGenerator.GenerateAsync(format, ct), requestedByUserId, cancellationToken);

    public Task<AuditLogReportResult> GenerateBundleReportAsync(string type, DateTime? from, DateTime? to, Guid? themeId, Guid requestedByUserId, string locale, string format = ReportFormatCodes.Xlsx, CancellationToken cancellationToken = default) =>
        GenerateReportAsync(
            type,
            $"{type}.{format}",
            format,
            async ct =>
            {
                var bundle = await _builder.BuildAsync(type, from, to, themeId, requestedByUserId.ToString(), ct);
                return format switch
                {
                    ReportFormatCodes.Pdf => _pdfRenderer.Render(bundle, locale),
                    ReportFormatCodes.Pptx => _pptxRenderer.Render(bundle, locale),
                    _ => _renderer.Render(bundle, locale),
                };
            },
            requestedByUserId,
            cancellationToken);

    private async Task<AuditLogReportResult> GenerateReportAsync(
        string reportTitleKey,
        string fileName,
        string format,
        Func<CancellationToken, Task<byte[]>> generate,
        Guid requestedByUserId,
        CancellationToken cancellationToken)
    {
        var reportTitle = await _db.ReportTitles.SingleAsync(r => r.Key == reportTitleKey, cancellationToken);

        var reportGeneration = new ReportGeneration
        {
            Id = Guid.NewGuid(),
            ReportTitleId = reportTitle.Id,
            Format = format,
            Status = ReportGenerationStatusCodes.Pending,
            RequestedById = requestedByUserId,
        };
        _db.ReportGenerations.Add(reportGeneration);
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var content = await generate(cancellationToken);
            var fileUrl = await _storage.SaveAsync(fileName, content, cancellationToken);

            reportGeneration.Status = ReportGenerationStatusCodes.Completed;
            reportGeneration.FileUrl = fileUrl;
            reportGeneration.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            reportGeneration.Status = ReportGenerationStatusCodes.Failed;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new AuditLogReportResult(reportGeneration.Id, reportGeneration.Status, reportGeneration.FileUrl);
    }
}
