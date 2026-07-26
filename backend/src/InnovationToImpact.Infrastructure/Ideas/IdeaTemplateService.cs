using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Ideas;

public class IdeaTemplateService : IIdeaTemplateService
{
    private readonly InnovationDbContext _db;
    private readonly IIdeaTemplateFileStorage _storage;

    public IdeaTemplateService(InnovationDbContext db, IIdeaTemplateFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<IdeaTemplate> ReplaceAsync(Stream content, string fileName, string contentType, Guid uploaderId, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        var storedPath = await _storage.SaveAsync(fileName, bytes, cancellationToken);

        var template = new IdeaTemplate
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            ContentType = contentType,
            StoredPath = storedPath,
            SizeBytes = bytes.LongLength,
            UploadedAt = DateTime.UtcNow,
            UploadedById = uploaderId,
        };
        _db.IdeaTemplates.Add(template);
        await _db.SaveChangesAsync(cancellationToken);
        return template;
    }

    public async Task<IdeaTemplate?> GetCurrentAsync(CancellationToken cancellationToken = default) =>
        await _db.IdeaTemplates
            .OrderByDescending(t => t.UploadedAt)
            .FirstOrDefaultAsync(cancellationToken);
}
