using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class CmsBlockConfiguration : IEntityTypeConfiguration<CmsBlock>
{
    public void Configure(EntityTypeBuilder<CmsBlock> builder)
    {
        builder.ToTable("CmsBlocks");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Key).IsRequired().HasMaxLength(150);
        builder.HasIndex(c => c.Key).IsUnique();

        builder.Property(c => c.ContentAr).IsRequired();
        builder.Property(c => c.ContentEn).IsRequired();

        builder.Property(c => c.IsPublished).HasDefaultValue(true);
    }
}
