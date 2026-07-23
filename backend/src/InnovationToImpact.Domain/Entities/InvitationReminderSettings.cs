namespace InnovationToImpact.Domain.Entities;

public class InvitationReminderSettings
{
    public Guid Id { get; set; }
    public bool Enabled { get; set; } = true;
    public string CronExpression { get; set; } = "0 9 * * 1";
    public string Timezone { get; set; } = "Asia/Riyadh";
    public int StopAfterNReminders { get; set; } = 3;
    public int GapHours { get; set; } = 48;
    public int ExpiresDays { get; set; } = 14;
    public string FromName { get; set; } = "Innovation-to-Impact Program";
    public string FromEmail { get; set; } = "noreply@gac.gov.sa";
    public string ProgramNameAr { get; set; } = "برنامج ابتكر لمنافس";
    public string ProgramNameEn { get; set; } = "Innovation-to-Impact Program";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedByUserId { get; set; }
}
