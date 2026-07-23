namespace InnovationToImpact.Domain.Entities;

public class ReportTitle
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
