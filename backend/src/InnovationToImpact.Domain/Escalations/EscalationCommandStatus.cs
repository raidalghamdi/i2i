namespace InnovationToImpact.Domain.Escalations;

public enum EscalationCommandStatus
{
    Success,
    NotFound,
    AlreadyMaxTier,
    InvalidStatusForAction,
    ResolutionRequired,
}
