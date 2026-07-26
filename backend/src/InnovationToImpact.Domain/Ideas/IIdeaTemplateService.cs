using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Ideas;

public interface IIdeaTemplateService
{
    Task<IdeaTemplate> ReplaceAsync(Stream content, string fileName, string contentType, Guid uploaderId, CancellationToken cancellationToken = default);
    Task<IdeaTemplate?> GetCurrentAsync(CancellationToken cancellationToken = default);
}
