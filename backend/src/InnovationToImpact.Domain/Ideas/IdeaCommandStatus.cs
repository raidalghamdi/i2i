namespace InnovationToImpact.Domain.Ideas;

public enum IdeaCommandStatus
{
    Success,
    NotFound,
    Forbidden,
    InvalidState,
    InvalidAttachment,
    InvalidStrategicTheme,
    InvalidActivity,
    InvalidChallenge,
    InvalidParticipation,
    ConsentRequired,
    SectionNotEditable,
}
