namespace InnovationToImpact.Domain.Assignments;

public sealed record BulkAssignmentCreateRequest(IReadOnlyList<AssignmentCreateInput> Assignments);
