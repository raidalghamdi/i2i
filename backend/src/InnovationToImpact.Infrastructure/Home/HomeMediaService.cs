using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Home;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Home;

public class HomeMediaService : IHomeMediaService
{
    private readonly InnovationDbContext _db;
    private readonly IHomeMediaFileStorage _storage;

    public HomeMediaService(InnovationDbContext db, IHomeMediaFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<HomeMedia> SaveAsync(Stream content, string fileName, string contentType, Guid? uploaderId, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        var storedPath = await _storage.SaveAsync(fileName, bytes, cancellationToken);

        var media = new HomeMedia
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            ContentType = contentType,
            StoredPath = storedPath,
            SizeBytes = bytes.LongLength,
            UploadedAt = DateTime.UtcNow,
            UploadedById = uploaderId,
        };
        _db.HomeMedias.Add(media);
        await _db.SaveChangesAsync(cancellationToken);
        return media;
    }

    public async Task<HomeMedia?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.HomeMedias.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
}
