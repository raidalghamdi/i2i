using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class SlaTrackingConfiguration : IEntityTypeConfiguration<SlaTracking>
{
    public void Configure(EntityTypeBuilder<SlaTracking> builder)
    {
        builder.ToTable("SlaTrackings");
        builder.HasKey(t => t.Id);

        // EntityId is a deliberately unconstrained polymorphic reference — no HasOne/HasForeignKey here.

        builder.HasOne(t => t.SlaPolicy)
            .WithMany()
            .HasForeignKey(t => t.SlaPolicyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
