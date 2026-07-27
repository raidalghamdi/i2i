namespace InnovationToImpact.Domain.Evaluations;

// Change 20260726
public sealed record EvaluationInput(
    Dictionary<string, decimal> CriteriaScores,
    string? Comments,
    string? Recommendation = null);
