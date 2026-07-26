namespace InnovationToImpact.Domain.Entities;

public class RoleInvitationSettings
{
    public Guid Id { get; set; }
    public bool Enabled { get; set; } = true;
    public int DefaultExpiresDays { get; set; } = 14;
    public int ReminderGapHours { get; set; } = 48;
    public int MaxReminders { get; set; } = 3;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedByUserId { get; set; }
}
