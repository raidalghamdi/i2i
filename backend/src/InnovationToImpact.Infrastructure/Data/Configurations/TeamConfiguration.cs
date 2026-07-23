using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.NameAr).IsRequired().HasMaxLength(300);
        builder.Property(t => t.NameEn).IsRequired().HasMaxLength(300);
        builder.Property(t => t.Slug).IsRequired().HasMaxLength(150);
        builder.HasIndex(t => t.Slug).IsUnique();

        builder.Property(t => t.IsActive).HasDefaultValue(true);

        builder.HasOne(t => t.Leader)
            .WithMany()
            .HasForeignKey(t => t.LeaderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
