namespace InnovationToImpact.Domain.Entities;

public class Challenge
{
    public Guid Id { get; set; }
    public Guid StrategicThemeId { get; set; }
    public StrategicTheme StrategicTheme { get; set; } = null!;
    public string TextAr { get; set; } = string.Empty;
    public string TextEn { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
