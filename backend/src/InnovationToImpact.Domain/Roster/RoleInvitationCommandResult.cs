using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Roster;

public sealed record RoleInvitationCommandResult(RoleInvitationCommandStatus Status, RoleInvitation? Entity = null, string? SamAccountName = null);
