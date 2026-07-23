namespace InnovationToImpact.Domain.Entities;

public class StrategicTheme
{
    public Guid Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public int Priority { get; set; }
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;
}
