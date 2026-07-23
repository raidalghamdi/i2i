namespace InnovationToImpact.Domain.Roster;

public sealed record RoleInvitationSettingsInput(
    bool? Enabled,
    int? DefaultExpiresDays,
    int? ReminderGapHours,
    int? MaxReminders
);
