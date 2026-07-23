namespace InnovationToImpact.Domain.Entities;

public class ContentString
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;
    public string ValueAr { get; set; } = string.Empty;
    public string ValueEn { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
