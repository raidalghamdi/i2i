using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Invitations;

public interface IInvitationReminderSettingsService
{
    Task<InvitationReminderSettings> GetAsync(CancellationToken cancellationToken = default);
    Task<InvitationReminderSettings> UpdateAsync(InvitationReminderSettingsInput input, Guid actorId, CancellationToken cancellationToken = default);
}
