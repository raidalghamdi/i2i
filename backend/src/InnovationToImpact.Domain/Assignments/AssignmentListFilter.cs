namespace InnovationToImpact.Domain.Assignments;

public sealed record AssignmentListFilter(Guid? EvaluatorId, string? StatusCode, string? IdeaSearch, int Page, int PageSize);
