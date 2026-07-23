namespace InnovationToImpact.Domain.Reports;

public interface IReportGenerationService
{
    Task<AuditLogReportResult> GenerateAuditLogReportAsync(Guid requestedByUserId, CancellationToken cancellationToken = default);
    Task<AuditLogReportResult> GenerateIdeasReportAsync(Guid requestedByUserId, CancellationToken cancellationToken = default);
    Task<AuditLogReportResult> GenerateEvaluationsReportAsync(Guid requestedByUserId, CancellationToken cancellationToken = default);
    Task<AuditLogReportResult> GenerateEscalationsReportAsync(Guid requestedByUserId, CancellationToken cancellationToken = default);
    Task<AuditLogReportResult> GenerateAnalyticsReportAsync(Guid requestedByUserId, string format, CancellationToken cancellationToken = default);
    Task<AuditLogReportResult> GenerateBundleReportAsync(string type, DateTime? from, DateTime? to, Guid? themeId, Guid requestedByUserId, string locale, string format = ReportFormatCodes.Xlsx, CancellationToken cancellationToken = default);
}
