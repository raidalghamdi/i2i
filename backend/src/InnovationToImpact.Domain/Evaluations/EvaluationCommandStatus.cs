namespace InnovationToImpact.Domain.Evaluations;

public enum EvaluationCommandStatus
{
    Success,
    NotFound,
    Forbidden,
    InvalidState,
    AlreadyEvaluated,
    InvalidScore,
    InvalidCriteria, // Change 20260726
    SelfAuthorship, // Change 20260726
}
