namespace InnovationToImpact.Domain.Assignments;

public sealed record WorkloadRow(Guid EvaluatorId, string EvaluatorName, int Pending, int DueSoon, int Overdue, int CompletedRecent);
