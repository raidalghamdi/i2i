using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class EvaluationConfiguration : IEntityTypeConfiguration<Evaluation>
{
    public void Configure(EntityTypeBuilder<Evaluation> builder)
    {
        builder.ToTable("Evaluations");
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.IdeaId, e.EvaluatorId }).IsUnique();

        builder.Property(e => e.CriteriaScoresJson).IsRequired();
        builder.Property(e => e.TotalScore).HasPrecision(6, 2);
        builder.Property(e => e.Comments).HasMaxLength(4000);
        builder.Property(e => e.Recommendation).HasMaxLength(2000);

        builder.HasOne(e => e.Idea)
            .WithMany()
            .HasForeignKey(e => e.IdeaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Evaluator)
            .WithMany()
            .HasForeignKey(e => e.EvaluatorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
