namespace InnovationToImpact.Domain.Reports;

public interface IAnalyticsReportGenerator
{
    Task<byte[]> GenerateAsync(string format, CancellationToken cancellationToken = default);
}
