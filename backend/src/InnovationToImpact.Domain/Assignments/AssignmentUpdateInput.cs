namespace InnovationToImpact.Domain.Assignments;

public sealed record AssignmentUpdateInput(string StatusCode, DateTime? DueAt, string? Notes, Guid EvaluatorId);
