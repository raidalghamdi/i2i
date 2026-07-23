using InnovationToImpact.Domain.Content;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Content;

public class PublicContentService : IPublicContentService
{
    private readonly InnovationDbContext _db;

    public PublicContentService(InnovationDbContext db)
    {
        _db = db;
    }

    public Task<CmsContent?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        _db.CmsContents.SingleOrDefaultAsync(c => c.Slug == slug && c.IsPublished, cancellationToken);
}
