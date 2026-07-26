namespace InnovationToImpact.Domain.EvaluationReview;

public sealed record EvaluationReviewDecisionInput(
    string DecisionCode,
    string? SupervisorComment,
    string? Reason,
    IReadOnlyList<string>? EditableSections = null);
