namespace InnovationToImpact.Domain.Invitations;

public sealed record InvitationReminderSettingsInput(
    bool? Enabled,
    string? CronExpression,
    string? Timezone,
    int? StopAfterNReminders,
    int? GapHours,
    int? ExpiresDays,
    string? FromName,
    string? FromEmail,
    string? ProgramNameAr,
    string? ProgramNameEn
);
