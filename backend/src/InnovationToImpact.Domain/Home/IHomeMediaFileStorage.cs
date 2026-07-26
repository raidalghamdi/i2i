namespace InnovationToImpact.Domain.Home;

public interface IHomeMediaFileStorage
{
    Task<string> SaveAsync(string fileName, byte[] content, CancellationToken cancellationToken = default);
    Task<byte[]> ReadAsync(string fileUrl, CancellationToken cancellationToken = default);
}
