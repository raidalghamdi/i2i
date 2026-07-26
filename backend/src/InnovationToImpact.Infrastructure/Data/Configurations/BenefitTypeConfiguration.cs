using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class BenefitTypeConfiguration : IEntityTypeConfiguration<BenefitType>
{
    private static readonly (string Code, string NameAr, string NameEn)[] SeedTypes =
    {
        ("quantitative", "كمي", "Quantitative"),
        ("qualitative", "نوعي", "Qualitative"),
    };

    public void Configure(EntityTypeBuilder<BenefitType> builder)
    {
        builder.ToTable("BenefitTypes");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(t => t.Code).IsUnique();
        builder.Property(t => t.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(t => t.NameEn).IsRequired().HasMaxLength(200);

        builder.HasData(SeedTypes.Select((t, index) => new BenefitType
        {
            Id = new Guid($"00000000-0000-0000-0005-{(index + 1):D12}"),
            Code = t.Code,
            NameAr = t.NameAr,
            NameEn = t.NameEn,
            SortOrder = index + 1,
        }));
    }
}
