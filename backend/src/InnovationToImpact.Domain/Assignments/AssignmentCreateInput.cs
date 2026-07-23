namespace InnovationToImpact.Domain.Assignments;

public sealed record AssignmentCreateInput(Guid IdeaId, Guid EvaluatorId, DateTime? DueAt, string? Notes);
