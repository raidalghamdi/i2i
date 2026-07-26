namespace InnovationToImpact.Domain.Evaluations;

public enum EvaluationCommandStatus
{
    Success,
    NotFound,
    Forbidden,
    InvalidState,
    AlreadyEvaluated,
    InvalidScore,
}
