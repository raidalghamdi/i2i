namespace InnovationToImpact.Domain.Roster;

public enum RoleInvitationCommandStatus
{
    Success,
    AlreadyApplied,
    AlreadyPending,
    AdUserNotFound,
    RoleNotFound,
    NotFound,
    InvalidStatus,
}
