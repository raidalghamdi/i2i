namespace InnovationToImpact.Domain.UserManagement;

public enum RoleGrantCommandStatus
{
    GrantedImmediately,
    Pending,
    RoleNotFound,
    AlreadyGranted,
    AlreadyPending,
    AdUserNotFound,
}
