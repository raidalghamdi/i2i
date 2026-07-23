using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.UserManagement;

public sealed record RoleGrantCommandResult(RoleGrantCommandStatus Status, User? User = null, PendingRoleGrant? PendingGrant = null);
