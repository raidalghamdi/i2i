using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Content;

public interface IPublicContentService
{
    Task<CmsContent?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
