namespace InnovationToImpact.Domain.Reports;

public interface IEscalationsReportGenerator
{
    Task<byte[]> GenerateAsync(CancellationToken cancellationToken = default);
}
