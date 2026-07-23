using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Roster;

public sealed record RosterRoleDetail(string RoleCode, string RoleNameAr, string RoleNameEn, IReadOnlyList<RosterActiveMember> ActiveMembers, IReadOnlyList<RoleInvitation> Invitations);
