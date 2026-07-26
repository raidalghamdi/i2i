using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class ContentStringConfiguration : IEntityTypeConfiguration<ContentString>
{
    public void Configure(EntityTypeBuilder<ContentString> builder)
    {
        builder.ToTable("ContentStrings");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Key).IsRequired().HasMaxLength(200);
        builder.HasIndex(s => s.Key).IsUnique();

        builder.Property(s => s.ValueAr).IsRequired();
        builder.Property(s => s.ValueEn).IsRequired();
    }
}
