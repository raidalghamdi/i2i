namespace InnovationToImpact.Domain.Screening;

public enum ScreeningCommandStatus
{
    Success,
    NotFound,
    InvalidState,
    ReasonRequired,
    InvalidDecision,
}
