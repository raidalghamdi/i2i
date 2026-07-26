using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class KnowledgeTypeConfiguration : IEntityTypeConfiguration<KnowledgeType>
{
    private static readonly (string Code, string NameAr, string NameEn)[] SeedTypes =
    {
        ("article", "مقال", "Article"),
        ("case_study", "دراسة حالة", "Case Study"),
        ("template", "نموذج", "Template"),
        ("official_guide", "دليل رسمي", "Official Guide"),
    };

    public void Configure(EntityTypeBuilder<KnowledgeType> builder)
    {
        builder.ToTable("KnowledgeTypes");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(t => t.Code).IsUnique();
        builder.Property(t => t.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(t => t.NameEn).IsRequired().HasMaxLength(200);

        builder.HasData(SeedTypes.Select((t, index) => new KnowledgeType
        {
            Id = new Guid($"00000000-0000-0000-0011-{(index + 1):D12}"),
            Code = t.Code,
            NameAr = t.NameAr,
            NameEn = t.NameEn,
            SortOrder = index + 1,
        }));
    }
}
