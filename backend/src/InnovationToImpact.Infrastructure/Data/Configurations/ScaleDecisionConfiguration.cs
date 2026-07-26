using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class ScaleDecisionConfiguration : IEntityTypeConfiguration<ScaleDecision>
{
    public void Configure(EntityTypeBuilder<ScaleDecision> builder)
    {
        builder.ToTable("ScaleDecisions");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.EvidenceOfViabilityAr).IsRequired();
        builder.Property(d => d.EvidenceOfViabilityEn).IsRequired();
        builder.Property(d => d.ValueAssessmentAr).IsRequired();
        builder.Property(d => d.ValueAssessmentEn).IsRequired();
        builder.Property(d => d.RiskAssessmentAr).IsRequired();
        builder.Property(d => d.RiskAssessmentEn).IsRequired();
        builder.Property(d => d.StrategicFitScore).HasPrecision(4, 2);

        builder.HasOne(d => d.Idea)
            .WithMany()
            .HasForeignKey(d => d.IdeaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.ScaleDecisionType)
            .WithMany()
            .HasForeignKey(d => d.ScaleDecisionTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.DecidedBy)
            .WithMany()
            .HasForeignKey(d => d.DecidedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
