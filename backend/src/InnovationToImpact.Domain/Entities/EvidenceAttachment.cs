namespace InnovationToImpact.Domain.Entities;

public class EvidenceAttachment
{
    public Guid Id { get; set; }

    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }

    public Guid UploaderId { get; set; }
    public User Uploader { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;
    public string BlobPath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}
