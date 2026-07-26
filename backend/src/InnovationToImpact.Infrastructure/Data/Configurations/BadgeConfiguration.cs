using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class BadgeConfiguration : IEntityTypeConfiguration<Badge>
{
    public void Configure(EntityTypeBuilder<Badge> builder)
    {
        builder.ToTable("Badges");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Code).IsRequired().HasMaxLength(100);
        builder.HasIndex(b => b.Code).IsUnique();

        builder.Property(b => b.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(b => b.NameEn).IsRequired().HasMaxLength(200);
        builder.Property(b => b.IconUrl).HasMaxLength(500);

        builder.Property(b => b.IsActive).HasDefaultValue(true);
    }
}
