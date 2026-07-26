namespace InnovationToImpact.Infrastructure.Storage;

public class LocalDiskFileStorage : InnovationToImpact.Domain.Ideas.IEvidenceFileStorage, InnovationToImpact.Domain.Reports.IReportFileStorage, InnovationToImpact.Domain.EmailTemplates.IEmailTemplateAttachmentFileStorage, InnovationToImpact.Domain.Ideas.IIdeaTemplateFileStorage, InnovationToImpact.Domain.Home.IHomeMediaFileStorage
{
    private readonly string _rootPath;

    public LocalDiskFileStorage(string rootPath)
    {
        _rootPath = rootPath;
    }

    public async Task<string> SaveAsync(string fileName, byte[] content, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_rootPath);

        // SECURITY: never build the physical path from the raw client-supplied file name. A name
        // containing "../" / "..\" (or an absolute/rooted path) would otherwise let an upload
        // traverse outside the storage root. Store under a random GUID plus only a sanitized
        // extension. The original display name is preserved by callers in their own DB column
        // (EvidenceAttachment.FileName, EmailTemplateAttachment.FileName, IdeaTemplate.FileName, …)
        // and surfaced via Content-Disposition — it is never recovered from this physical path.
        var ext = Path.GetExtension(fileName ?? string.Empty);
        // Path.GetExtension only returns the trailing ".xyz", but guard anyway against a separator
        // sneaking in or an absurdly long extension.
        if (ext.Length > 20 || ext.IndexOfAny(new[] { '/', '\\' }) >= 0)
        {
            ext = string.Empty;
        }
        var storedFileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(_rootPath, storedFileName);

        // Defense in depth: verify the resolved path stays within the storage root.
        var rootFull = Path.GetFullPath(_rootPath);
        var rootWithSep = rootFull.EndsWith(Path.DirectorySeparatorChar)
            ? rootFull
            : rootFull + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(fullPath).StartsWith(rootWithSep, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Resolved storage path escaped the storage root.");
        }

        await File.WriteAllBytesAsync(fullPath, content, cancellationToken);
        return fullPath;
    }

    public Task<byte[]> ReadAsync(string fileUrl, CancellationToken cancellationToken = default) =>
        File.ReadAllBytesAsync(fileUrl, cancellationToken);

    public Task DeleteAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (File.Exists(filePath)) File.Delete(filePath);
        return Task.CompletedTask;
    }
}
