namespace InnovationToImpact.Domain.Entities;

public class SlaPolicy
{
    public Guid Id { get; set; }

    public string EntityType { get; set; } = string.Empty;
    public string FromState { get; set; } = string.Empty;
    public string ToState { get; set; } = string.Empty;

    public int TargetHours { get; set; }
    public int WarnAtPct { get; set; }
}
