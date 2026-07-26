using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class RoleInvitationSettingsConfiguration : IEntityTypeConfiguration<RoleInvitationSettings>
{
    public static readonly Guid SingletonId = Guid.Parse("22222222-3333-4444-5555-666666666666");

    public void Configure(EntityTypeBuilder<RoleInvitationSettings> builder)
    {
        builder.ToTable("RoleInvitationSettings");
        builder.HasKey(s => s.Id);

        builder.HasData(new RoleInvitationSettings
        {
            Id = SingletonId,
            Enabled = true,
            DefaultExpiresDays = 14,
            ReminderGapHours = 48,
            MaxReminders = 3,
            UpdatedAt = new DateTime(2026, 7, 21, 0, 0, 0, DateTimeKind.Utc),
            UpdatedByUserId = null,
        });
    }
}
