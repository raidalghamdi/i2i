namespace InnovationToImpact.Domain.Ideas;

public interface IEvidenceFileStorage
{
    Task<string> SaveAsync(string fileName, byte[] content, CancellationToken cancellationToken = default);
}
