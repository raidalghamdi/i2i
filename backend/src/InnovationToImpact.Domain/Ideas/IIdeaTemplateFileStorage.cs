namespace InnovationToImpact.Domain.Ideas;

public interface IIdeaTemplateFileStorage
{
    Task<string> SaveAsync(string fileName, byte[] content, CancellationToken cancellationToken = default);
    Task<byte[]> ReadAsync(string fileUrl, CancellationToken cancellationToken = default);
}
