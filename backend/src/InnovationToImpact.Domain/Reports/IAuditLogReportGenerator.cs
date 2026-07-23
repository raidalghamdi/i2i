namespace InnovationToImpact.Domain.Reports;

public interface IAuditLogReportGenerator
{
    Task<byte[]> GenerateAsync(CancellationToken cancellationToken = default);
}
