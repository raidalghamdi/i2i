namespace InnovationToImpact.Domain.Entities;

public class CmsContent
{
    public Guid Id { get; set; }

    public string Slug { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string BodyAr { get; set; } = string.Empty;
    public string BodyEn { get; set; } = string.Empty;

    public bool IsPublished { get; set; } = true;
    public DateTime? PublishedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
