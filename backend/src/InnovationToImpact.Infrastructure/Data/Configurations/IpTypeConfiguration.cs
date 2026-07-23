using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class IpTypeConfiguration : IEntityTypeConfiguration<IpType>
{
    private static readonly (string Code, string NameAr, string NameEn)[] SeedTypes =
    {
        ("patent", "براءة اختراع", "Patent"),
        ("trademark", "علامة تجارية", "Trademark"),
        ("copyright", "حقوق النشر", "Copyright"),
        ("trade_secret", "سر تجاري", "Trade Secret"),
    };

    public void Configure(EntityTypeBuilder<IpType> builder)
    {
        builder.ToTable("IpTypes");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(t => t.Code).IsUnique();
        builder.Property(t => t.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(t => t.NameEn).IsRequired().HasMaxLength(200);

        builder.HasData(SeedTypes.Select((t, index) => new IpType
        {
            Id = new Guid($"00000000-0000-0000-0010-{(index + 1):D12}"),
            Code = t.Code,
            NameAr = t.NameAr,
            NameEn = t.NameEn,
            SortOrder = index + 1,
        }));
    }
}
