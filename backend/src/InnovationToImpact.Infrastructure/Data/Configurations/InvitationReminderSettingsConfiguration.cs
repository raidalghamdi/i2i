using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class InvitationReminderSettingsConfiguration : IEntityTypeConfiguration<InvitationReminderSettings>
{
    public static readonly Guid SingletonId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    public void Configure(EntityTypeBuilder<InvitationReminderSettings> builder)
    {
        builder.ToTable("InvitationReminderSettings");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.CronExpression).IsRequired();
        builder.Property(s => s.Timezone).IsRequired();
        builder.Property(s => s.FromName).IsRequired();
        builder.Property(s => s.FromEmail).IsRequired();
        builder.Property(s => s.ProgramNameAr).IsRequired();
        builder.Property(s => s.ProgramNameEn).IsRequired();

        builder.HasData(new InvitationReminderSettings
        {
            Id = SingletonId,
            Enabled = true,
            CronExpression = "0 9 * * 1",
            Timezone = "Asia/Riyadh",
            StopAfterNReminders = 3,
            GapHours = 48,
            ExpiresDays = 14,
            FromName = "Innovation-to-Impact Program",
            FromEmail = "noreply@gac.gov.sa",
            ProgramNameAr = "برنامج ابتكر لمنافس",
            ProgramNameEn = "Innovation-to-Impact Program",
            UpdatedAt = new DateTime(2026, 7, 19, 0, 0, 0, DateTimeKind.Utc),
            UpdatedByUserId = null,
        });
    }
}
