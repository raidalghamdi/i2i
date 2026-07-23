namespace InnovationToImpact.Domain.Reports;

public interface IReportFileStorage
{
    Task<string> SaveAsync(string fileName, byte[] content, CancellationToken cancellationToken = default);
    Task<byte[]> ReadAsync(string fileUrl, CancellationToken cancellationToken = default);
}
