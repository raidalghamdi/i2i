namespace InnovationToImpact.Domain.Entities;

public class Activity
{
    public Guid Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;
}
