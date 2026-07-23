using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class ReportGenerationConfiguration : IEntityTypeConfiguration<ReportGeneration>
{
    public void Configure(EntityTypeBuilder<ReportGeneration> builder)
    {
        builder.ToTable("ReportGenerations");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Format).IsRequired().HasMaxLength(50);
        builder.Property(g => g.Status).IsRequired().HasMaxLength(50);
        builder.Property(g => g.FileUrl).HasMaxLength(500);

        builder.HasOne(g => g.ReportTitle)
            .WithMany()
            .HasForeignKey(g => g.ReportTitleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.RequestedBy)
            .WithMany()
            .HasForeignKey(g => g.RequestedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
