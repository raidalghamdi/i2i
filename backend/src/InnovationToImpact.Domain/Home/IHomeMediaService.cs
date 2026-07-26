using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Home;

public interface IHomeMediaService
{
    Task<HomeMedia> SaveAsync(Stream content, string fileName, string contentType, Guid? uploaderId, CancellationToken cancellationToken = default);
    Task<HomeMedia?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
