using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class EmailLogConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.ToTable("EmailLogs");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Provider).IsRequired().HasMaxLength(50);
        builder.Property(l => l.ProviderMessageId).HasMaxLength(300);
        builder.Property(l => l.RelatedEntityType).HasMaxLength(100);
        builder.Property(l => l.ToEmail).IsRequired().HasMaxLength(320);

        // RelatedEntityId is a deliberately unconstrained polymorphic reference — no HasOne/HasForeignKey here.

        builder.HasOne(l => l.ToUser)
            .WithMany()
            .HasForeignKey(l => l.ToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.EmailLogStatus)
            .WithMany()
            .HasForeignKey(l => l.EmailLogStatusId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
