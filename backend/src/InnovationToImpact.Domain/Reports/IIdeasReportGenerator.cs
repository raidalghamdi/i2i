namespace InnovationToImpact.Domain.Reports;

public interface IIdeasReportGenerator
{
    Task<byte[]> GenerateAsync(CancellationToken cancellationToken = default);
}
