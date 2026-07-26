using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.NotificationType).IsRequired().HasMaxLength(100);
        builder.Property(n => n.TitleAr).IsRequired().HasMaxLength(300);
        builder.Property(n => n.TitleEn).IsRequired().HasMaxLength(300);
        builder.Property(n => n.BodyAr).IsRequired();
        builder.Property(n => n.BodyEn).IsRequired();
        builder.Property(n => n.Link).HasMaxLength(2000);

        builder.HasIndex(n => new { n.UserId, n.ReadAt });

        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
