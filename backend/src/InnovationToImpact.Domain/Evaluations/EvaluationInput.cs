namespace InnovationToImpact.Domain.Evaluations;

// Change 20260726
public static class EvaluationActions
{
    public const string Draft = "draft";
    public const string Submit = "submit";
}

// Change 20260726
public sealed record EvaluationInput(
    Dictionary<string, decimal> CriteriaScores,
    string? Comments,
    string? Recommendation = null,
    string? Action = null,
    bool ConflictOfInterest = false);
