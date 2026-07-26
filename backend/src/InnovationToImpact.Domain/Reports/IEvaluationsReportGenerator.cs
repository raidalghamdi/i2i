namespace InnovationToImpact.Domain.Reports;

public interface IEvaluationsReportGenerator
{
    Task<byte[]> GenerateAsync(CancellationToken cancellationToken = default);
}
