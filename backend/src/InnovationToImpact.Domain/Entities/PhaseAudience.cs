namespace InnovationToImpact.Domain.Entities;

public class PhaseAudience
{
    public Guid Id { get; set; }
    public int PhaseIdx { get; set; }
    public string RoleCode { get; set; } = string.Empty;
}
