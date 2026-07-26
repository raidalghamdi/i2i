namespace InnovationToImpact.Domain.Entities;

public class CmsBlock
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;
    public string ContentAr { get; set; } = string.Empty;
    public string ContentEn { get; set; } = string.Empty;

    public bool IsPublished { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
