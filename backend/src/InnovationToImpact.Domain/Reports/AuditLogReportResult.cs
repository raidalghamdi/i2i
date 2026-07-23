namespace InnovationToImpact.Domain.Reports;

public sealed record AuditLogReportResult(Guid ReportGenerationId, string Status, string? FileUrl);
