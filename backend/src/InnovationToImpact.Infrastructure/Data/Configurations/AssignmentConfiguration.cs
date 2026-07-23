using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignments");
        builder.HasKey(a => a.Id);

        builder.HasIndex(a => new { a.EvaluatorId, a.AssignmentStatusId });

        builder.Property(a => a.Notes).HasMaxLength(2000);

        builder.HasOne(a => a.Idea)
            .WithMany()
            .HasForeignKey(a => a.IdeaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Evaluator)
            .WithMany()
            .HasForeignKey(a => a.EvaluatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.AssignedBy)
            .WithMany()
            .HasForeignKey(a => a.AssignedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.AssignmentStatus)
            .WithMany()
            .HasForeignKey(a => a.AssignmentStatusId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
