using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class ImplementationConfiguration : IEntityTypeConfiguration<Implementation>
{
    public void Configure(EntityTypeBuilder<Implementation> builder)
    {
        builder.ToTable("Implementations");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.IntegrationPlanAr).IsRequired();
        builder.Property(i => i.IntegrationPlanEn).IsRequired();
        builder.Property(i => i.ResourceCommitmentAr).IsRequired();
        builder.Property(i => i.ResourceCommitmentEn).IsRequired();
        builder.Property(i => i.LineUnit).HasMaxLength(200);

        builder.HasOne(i => i.Idea)
            .WithMany()
            .HasForeignKey(i => i.IdeaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.OperationalOwner)
            .WithMany()
            .HasForeignKey(i => i.OperationalOwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.HandoverStatus)
            .WithMany()
            .HasForeignKey(i => i.HandoverStatusId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
