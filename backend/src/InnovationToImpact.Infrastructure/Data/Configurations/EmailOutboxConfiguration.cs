using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class EmailOutboxConfiguration : IEntityTypeConfiguration<EmailOutbox>
{
    public void Configure(EntityTypeBuilder<EmailOutbox> builder)
    {
        builder.ToTable("EmailOutboxes");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.ToEmail).IsRequired().HasMaxLength(320);
        builder.Property(o => o.Subject).IsRequired().HasMaxLength(500);
        builder.Property(o => o.BodyHtml).IsRequired();
        builder.Property(o => o.Category).IsRequired().HasMaxLength(100);
        builder.Property(o => o.Attempts).HasDefaultValue(0);

        builder.HasOne(o => o.ToUser)
            .WithMany()
            .HasForeignKey(o => o.ToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.EmailOutboxStatus)
            .WithMany()
            .HasForeignKey(o => o.EmailOutboxStatusId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
