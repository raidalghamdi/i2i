using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class PostProgramHistoryConfiguration : IEntityTypeConfiguration<PostProgramHistory>
{
    public void Configure(EntityTypeBuilder<PostProgramHistory> builder)
    {
        builder.ToTable("PostProgramHistories");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.FromStage).IsRequired().HasMaxLength(50);
        builder.Property(h => h.ToStage).IsRequired().HasMaxLength(50);
        builder.Property(h => h.Comment).IsRequired().HasMaxLength(2000);

        builder.HasOne(h => h.Idea)
            .WithMany()
            .HasForeignKey(h => h.IdeaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.ChangedBy)
            .WithMany()
            .HasForeignKey(h => h.ChangedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
