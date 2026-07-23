namespace InnovationToImpact.Domain.Entities;

public class Assignment
{
    public Guid Id { get; set; }

    public Guid IdeaId { get; set; }
    public Idea Idea { get; set; } = null!;

    public Guid EvaluatorId { get; set; }
    public User Evaluator { get; set; } = null!;

    public Guid AssignedById { get; set; }
    public User AssignedBy { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueAt { get; set; }

    public Guid AssignmentStatusId { get; set; }
    public AssignmentStatus AssignmentStatus { get; set; } = null!;

    public string? Notes { get; set; }
}
