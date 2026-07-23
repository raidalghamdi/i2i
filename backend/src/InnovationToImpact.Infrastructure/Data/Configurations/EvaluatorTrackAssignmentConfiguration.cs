using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class EvaluatorTrackAssignmentConfiguration : IEntityTypeConfiguration<EvaluatorTrackAssignment>
{
    public void Configure(EntityTypeBuilder<EvaluatorTrackAssignment> builder)
    {
        builder.ToTable("EvaluatorTrackAssignments");
        builder.HasKey(a => a.Id);

        builder.HasIndex(a => new { a.EvaluatorId, a.TrackId }).IsUnique();

        builder.HasOne(a => a.Evaluator)
            .WithMany()
            .HasForeignKey(a => a.EvaluatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Track)
            .WithMany()
            .HasForeignKey(a => a.TrackId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.AssignedBy)
            .WithMany()
            .HasForeignKey(a => a.AssignedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
