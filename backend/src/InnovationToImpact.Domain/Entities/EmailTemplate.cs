namespace InnovationToImpact.Domain.Entities;

public class EmailTemplate
{
    public Guid Id { get; set; }

    public string Kind { get; set; } = string.Empty;

    public Guid? RoleId { get; set; }
    public Role? Role { get; set; }

    public string SubjectAr { get; set; } = string.Empty;
    public string SubjectEn { get; set; } = string.Empty;
    public string BodyAr { get; set; } = string.Empty;
    public string BodyEn { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public bool IsBroadcast { get; set; }
}
