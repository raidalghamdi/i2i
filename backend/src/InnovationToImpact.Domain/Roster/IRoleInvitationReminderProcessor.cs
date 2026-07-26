namespace InnovationToImpact.Domain.Roster;

public interface IRoleInvitationReminderProcessor
{
    Task<RoleInvitationReminderResult> ProcessAsync(CancellationToken cancellationToken = default);
}

public sealed record RoleInvitationReminderResult(int Scanned, int Expired, int RemindersQueued);
