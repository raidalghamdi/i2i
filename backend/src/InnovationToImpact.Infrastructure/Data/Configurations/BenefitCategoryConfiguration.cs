using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class BenefitCategoryConfiguration : IEntityTypeConfiguration<BenefitCategory>
{
    private static readonly (string Code, string NameAr, string NameEn)[] SeedCategories =
    {
        ("financial", "مالي", "Financial"),
        ("operational", "تشغيلي", "Operational"),
        ("social", "اجتماعي", "Social"),
        ("strategic", "استراتيجي", "Strategic"),
    };

    public void Configure(EntityTypeBuilder<BenefitCategory> builder)
    {
        builder.ToTable("BenefitCategories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(c => c.Code).IsUnique();
        builder.Property(c => c.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(c => c.NameEn).IsRequired().HasMaxLength(200);

        builder.HasData(SeedCategories.Select((c, index) => new BenefitCategory
        {
            Id = new Guid($"00000000-0000-0000-0006-{(index + 1):D12}"),
            Code = c.Code,
            NameAr = c.NameAr,
            NameEn = c.NameEn,
            SortOrder = index + 1,
        }));
    }
}
