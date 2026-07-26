namespace InnovationToImpact.Domain.Entities;

public class AdminSetting
{
    public string Key { get; set; } = string.Empty;
    public string ValueJson { get; set; } = "null";

    public Guid? UpdatedById { get; set; }
    public User? UpdatedBy { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
