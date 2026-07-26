namespace InnovationToImpact.Domain.Entities;

public class EmailTemplateAttachment
{
    public Guid Id { get; set; }

    public Guid EmailTemplateId { get; set; }
    public EmailTemplate EmailTemplate { get; set; } = null!;

    public Guid UploaderId { get; set; }
    public User Uploader { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;
    public string BlobPath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
