namespace InnovationToImpact.Domain.Invitations;

public interface IInvitationReminderProcessor
{
    Task<InvitationReminderResult> ProcessAsync(CancellationToken cancellationToken = default);
}
