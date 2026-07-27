using InnovationToImpact.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

// Change 20260726
public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("NotificationPreferences");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.CategoryKey).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Muted).HasDefaultValue(false);
        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => new { p.UserId, p.CategoryKey }).IsUnique();
    }
}
