using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class BenefitConfiguration : IEntityTypeConfiguration<Benefit>
{
    public void Configure(EntityTypeBuilder<Benefit> builder)
    {
        builder.ToTable("Benefits");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.TitleAr).IsRequired().HasMaxLength(500);
        builder.Property(b => b.TitleEn).IsRequired().HasMaxLength(500);
        builder.Property(b => b.MeasurementUnit).HasMaxLength(50);
        builder.Property(b => b.TargetValue).HasPrecision(16, 2);
        builder.Property(b => b.RealizedValue).HasPrecision(16, 2);

        builder.HasOne(b => b.Idea)
            .WithMany()
            .HasForeignKey(b => b.IdeaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.BenefitType)
            .WithMany()
            .HasForeignKey(b => b.BenefitTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.BenefitCategory)
            .WithMany()
            .HasForeignKey(b => b.BenefitCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.VerifiedBy)
            .WithMany()
            .HasForeignKey(b => b.VerifiedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
