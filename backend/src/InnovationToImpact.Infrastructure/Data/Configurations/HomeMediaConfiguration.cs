using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class HomeMediaConfiguration : IEntityTypeConfiguration<HomeMedia>
{
    public void Configure(EntityTypeBuilder<HomeMedia> builder)
    {
        builder.ToTable("HomeMedias");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.FileName).IsRequired().HasMaxLength(255);
        builder.Property(m => m.ContentType).IsRequired().HasMaxLength(150);
        builder.Property(m => m.StoredPath).IsRequired().HasMaxLength(500);

        builder.HasIndex(m => m.UploadedAt);
    }
}
