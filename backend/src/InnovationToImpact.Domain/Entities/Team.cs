namespace InnovationToImpact.Domain.Entities;

public class Team
{
    public Guid Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    public Guid LeaderId { get; set; }
    public User Leader { get; set; } = null!;

    public bool IsActive { get; set; } = true;
}
