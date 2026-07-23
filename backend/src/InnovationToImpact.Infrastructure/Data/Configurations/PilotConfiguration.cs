using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class PilotConfiguration : IEntityTypeConfiguration<Pilot>
{
    public void Configure(EntityTypeBuilder<Pilot> builder)
    {
        builder.ToTable("Pilots");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.HypothesisAr).IsRequired();
        builder.Property(p => p.HypothesisEn).IsRequired();
        builder.Property(p => p.ExperimentPlanAr).IsRequired();
        builder.Property(p => p.ExperimentPlanEn).IsRequired();
        builder.Property(p => p.Budget).HasPrecision(14, 2);

        builder.HasOne(p => p.Idea)
            .WithMany()
            .HasForeignKey(p => p.IdeaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.PilotStatus)
            .WithMany()
            .HasForeignKey(p => p.PilotStatusId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
