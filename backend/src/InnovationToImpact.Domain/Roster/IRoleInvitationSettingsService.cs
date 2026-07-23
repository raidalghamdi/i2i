using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Roster;

public interface IRoleInvitationSettingsService
{
    Task<RoleInvitationSettings> GetAsync(CancellationToken cancellationToken = default);
    Task<RoleInvitationSettings> UpdateAsync(RoleInvitationSettingsInput input, Guid actorId, CancellationToken cancellationToken = default);
}
