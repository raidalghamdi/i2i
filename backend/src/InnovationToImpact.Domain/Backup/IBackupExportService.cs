namespace InnovationToImpact.Domain.Backup;

public interface IBackupExportService
{
    Task<BackupExportResult> GenerateAsync(CancellationToken cancellationToken = default);
}

public sealed record BackupExportResult(byte[] Content, int SheetCount, int RowCount);
