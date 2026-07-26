namespace InnovationToImpact.Domain.Entities;

public class HomeMedia
{
    public Guid Id { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public Guid? UploadedById { get; set; }
}
