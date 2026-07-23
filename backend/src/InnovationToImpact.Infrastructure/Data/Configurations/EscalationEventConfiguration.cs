using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class EscalationEventConfiguration : IEntityTypeConfiguration<EscalationEvent>
{
    public void Configure(EntityTypeBuilder<EscalationEvent> builder)
    {
        builder.ToTable("EscalationEvents");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventType).IsRequired().HasMaxLength(100);

        builder.HasOne(e => e.Escalation)
            .WithMany()
            .HasForeignKey(e => e.EscalationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.FromTier)
            .WithMany()
            .HasForeignKey(e => e.FromTierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ToTier)
            .WithMany()
            .HasForeignKey(e => e.ToTierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Actor)
            .WithMany()
            .HasForeignKey(e => e.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
