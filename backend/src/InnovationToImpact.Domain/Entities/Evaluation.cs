namespace InnovationToImpact.Domain.Entities;

public class Evaluation
{
    public Guid Id { get; set; }

    public Guid IdeaId { get; set; }
    public Idea Idea { get; set; } = null!;

    public Guid EvaluatorId { get; set; }
    public User Evaluator { get; set; } = null!;

    public string CriteriaScoresJson { get; set; } = "{}";
    public decimal TotalScore { get; set; }
    public string? Comments { get; set; }
    public string? Recommendation { get; set; }
    public bool ConflictOfInterest { get; set; }
    public DateTime? SubmittedAt { get; set; }
}
