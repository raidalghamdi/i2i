namespace InnovationToImpact.Domain.Invitations;

public sealed record InvitationReminderResult(int Scanned, int Expired, int RemindersQueued);
