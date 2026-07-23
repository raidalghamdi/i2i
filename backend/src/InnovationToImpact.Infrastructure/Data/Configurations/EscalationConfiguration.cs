using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class EscalationConfiguration : IEntityTypeConfiguration<Escalation>
{
    public void Configure(EntityTypeBuilder<Escalation> builder)
    {
        builder.ToTable("Escalations");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.ReasonAr).IsRequired();
        builder.Property(e => e.ReasonEn).IsRequired();

        // EntityId is a deliberately unconstrained polymorphic reference — no HasOne/HasForeignKey here.

        builder.HasOne(e => e.EscalationTier)
            .WithMany()
            .HasForeignKey(e => e.EscalationTierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.EscalationStatus)
            .WithMany()
            .HasForeignKey(e => e.EscalationStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Owner)
            .WithMany()
            .HasForeignKey(e => e.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
