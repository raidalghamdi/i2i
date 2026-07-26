using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Ideas;

/// <summary>
/// Seeds the initial "current" idea-description template from a backend-embedded copy of the
/// .docx that used to live as a static frontend asset (frontend/public/templates/idea-description-template.docx).
/// The backend can't reliably read the frontend's folder at runtime, so that file is copied into
/// src/InnovationToImpact.Api/SeedAssets/idea-description-template.docx (content, CopyToOutputDirectory)
/// and this seeder reads it from there on first run, once, only if no IdeaTemplate row exists yet.
/// </summary>
public static class IdeaTemplateSeeder
{
    public const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public static async Task SeedIfMissingAsync(InnovationDbContext db, IIdeaTemplateFileStorage storage, string seedAssetPath, Guid uploaderId, CancellationToken cancellationToken = default)
    {
        if (await db.IdeaTemplates.AnyAsync(cancellationToken)) return;
        if (!File.Exists(seedAssetPath)) return;

        var bytes = await File.ReadAllBytesAsync(seedAssetPath, cancellationToken);
        var fileName = Path.GetFileName(seedAssetPath);
        var storedPath = await storage.SaveAsync(fileName, bytes, cancellationToken);

        db.IdeaTemplates.Add(new IdeaTemplate
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            ContentType = DocxContentType,
            StoredPath = storedPath,
            SizeBytes = bytes.LongLength,
            UploadedAt = DateTime.UtcNow,
            UploadedById = uploaderId,
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
