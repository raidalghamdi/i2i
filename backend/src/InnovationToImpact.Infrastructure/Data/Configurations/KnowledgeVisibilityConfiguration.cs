using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class KnowledgeVisibilityConfiguration : IEntityTypeConfiguration<KnowledgeVisibility>
{
    private static readonly (string Code, string NameAr, string NameEn)[] SeedVisibilities =
    {
        ("public", "عام", "Public"),
        ("internal", "داخلي", "Internal"),
    };

    public void Configure(EntityTypeBuilder<KnowledgeVisibility> builder)
    {
        builder.ToTable("KnowledgeVisibilities");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(v => v.Code).IsUnique();
        builder.Property(v => v.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(v => v.NameEn).IsRequired().HasMaxLength(200);

        builder.HasData(SeedVisibilities.Select((v, index) => new KnowledgeVisibility
        {
            Id = new Guid($"00000000-0000-0000-0012-{(index + 1):D12}"),
            Code = v.Code,
            NameAr = v.NameAr,
            NameEn = v.NameEn,
            SortOrder = index + 1,
        }));
    }
}
