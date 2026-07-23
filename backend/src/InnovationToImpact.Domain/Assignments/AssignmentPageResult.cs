using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Assignments;

public sealed record AssignmentPageResult(IReadOnlyList<Assignment> Items, int Total, int Page, int PageSize);
