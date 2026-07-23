namespace InnovationToImpact.Domain.Entities;

public class EvaluatorTrackAssignment
{
    public Guid Id { get; set; }

    public Guid EvaluatorId { get; set; }
    public User Evaluator { get; set; } = null!;

    public Guid TrackId { get; set; }
    public StrategicTheme Track { get; set; } = null!;

    public Guid AssignedById { get; set; }
    public User AssignedBy { get; set; } = null!;
}
