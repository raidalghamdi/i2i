namespace InnovationToImpact.Domain.Committee;

public enum CommitteeCommandStatus
{
    Success,
    NotFound,
    InvalidState,
    AlreadyDecided,
    InvalidDecisionType,
    InvalidCriteria,
    Forbidden,
    InvalidAttachment, // Change 20260726
}
