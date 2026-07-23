using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Cms;

public interface ICmsService
{
    Task<IReadOnlyList<CmsBlock>> ListBlocksAsync(CancellationToken cancellationToken = default);
    Task<CmsBlock?> GetBlockAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CmsCommandResult<CmsBlock>> CreateBlockAsync(CmsBlockInput input, Guid actorId, CancellationToken cancellationToken = default);
    Task<CmsCommandResult<CmsBlock>> UpdateBlockAsync(Guid id, CmsBlockInput input, Guid actorId, CancellationToken cancellationToken = default);
    Task<CmsCommandResult<CmsBlock>> DeleteBlockAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CmsContent>> ListContentAsync(CancellationToken cancellationToken = default);
    Task<CmsContent?> GetContentAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CmsCommandResult<CmsContent>> CreateContentAsync(CmsContentInput input, Guid actorId, CancellationToken cancellationToken = default);
    Task<CmsCommandResult<CmsContent>> UpdateContentAsync(Guid id, CmsContentInput input, Guid actorId, CancellationToken cancellationToken = default);
    Task<CmsCommandResult<CmsContent>> DeleteContentAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContentString>> ListStringsAsync(CancellationToken cancellationToken = default);
    Task<ContentString?> GetStringAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CmsCommandResult<ContentString>> CreateStringAsync(ContentStringInput input, Guid actorId, CancellationToken cancellationToken = default);
    Task<CmsCommandResult<ContentString>> UpdateStringAsync(Guid id, ContentStringInput input, Guid actorId, CancellationToken cancellationToken = default);
    Task<CmsCommandResult<ContentString>> DeleteStringAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default);
}
