namespace InnovationToImpact.Domain.Evaluations;

public sealed record EvaluationInput(
    decimal Innovation,
    decimal Impact,
    decimal Execution,
    decimal Scalability,
    decimal Presentation,
    string? Comments);
