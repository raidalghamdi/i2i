namespace InnovationToImpact.Domain.Committee;

public enum CommitteeCommandStatus
{
    Success,
    NotFound,
    InvalidState,
    AlreadyDecided,
    InvalidDecisionType,
    InvalidCriteria,
}
