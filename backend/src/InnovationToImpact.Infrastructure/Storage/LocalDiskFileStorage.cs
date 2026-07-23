namespace InnovationToImpact.Infrastructure.Storage;

public class LocalDiskFileStorage : InnovationToImpact.Domain.Ideas.IEvidenceFileStorage, InnovationToImpact.Domain.Reports.IReportFileStorage, InnovationToImpact.Domain.EmailTemplates.IEmailTemplateAttachmentFileStorage
{
    private readonly string _rootPath;

    public LocalDiskFileStorage(string rootPath)
    {
        _rootPath = rootPath;
    }

    public async Task<string> SaveAsync(string fileName, byte[] content, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_rootPath);

        // Path traversal defense -- DO NOT SIMPLIFY.
        // GetFileName() strips path separators, but on Linux it treats "\" as a literal
        // character (not a separator), so "..\..\evil" survives GetFileName intact.
        // The Contains("..") check is the actual cross-platform guard. Both layers required.
        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName) || safeFileName.Contains("..", StringComparison.Ordinal))
        {
            safeFileName = "upload";
        }

        var storedFileName = $"{Guid.NewGuid():N}-{safeFileName}";
        var fullPath = Path.Combine(_rootPath, storedFileName);
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
