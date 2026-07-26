using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class CommitteeDecisionConfiguration : IEntityTypeConfiguration<CommitteeDecision>
{
    public void Configure(EntityTypeBuilder<CommitteeDecision> builder)
    {
        builder.ToTable("CommitteeDecisions");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.CommitteeName).IsRequired().HasMaxLength(300);
        builder.Property(d => d.Comments).HasMaxLength(4000);
        builder.Property(d => d.CriteriaScoresJson).IsRequired();
        builder.Property(d => d.TotalScore).HasPrecision(5, 2);
        builder.HasIndex(d => new { d.IdeaId, d.DecidedById }).IsUnique();

        builder.HasOne(d => d.Idea)
            .WithMany()
            .HasForeignKey(d => d.IdeaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.CommitteeDecisionType)
            .WithMany()
            .HasForeignKey(d => d.CommitteeDecisionTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.DecidedBy)
            .WithMany()
            .HasForeignKey(d => d.DecidedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
