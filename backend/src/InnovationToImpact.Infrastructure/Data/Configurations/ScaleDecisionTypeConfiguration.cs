using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class ScaleDecisionTypeConfiguration : IEntityTypeConfiguration<ScaleDecisionType>
{
    private static readonly (string Code, string NameAr, string NameEn)[] SeedTypes =
    {
        ("scale", "توسيع", "Scale"),
        ("hold", "تعليق", "Hold"),
        ("reject", "رفض", "Reject"),
    };

    public void Configure(EntityTypeBuilder<ScaleDecisionType> builder)
    {
        builder.ToTable("ScaleDecisionTypes");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(t => t.Code).IsUnique();
        builder.Property(t => t.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(t => t.NameEn).IsRequired().HasMaxLength(200);

        builder.HasData(SeedTypes.Select((t, index) => new ScaleDecisionType
        {
            Id = new Guid($"00000000-0000-0000-0008-{(index + 1):D12}"),
            Code = t.Code,
            NameAr = t.NameAr,
            NameEn = t.NameEn,
            SortOrder = index + 1,
        }));
    }
}
