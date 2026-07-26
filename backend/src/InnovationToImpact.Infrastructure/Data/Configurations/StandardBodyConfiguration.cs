using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class StandardBodyConfiguration : IEntityTypeConfiguration<StandardBody>
{
    private static readonly (string Code, string NameAr, string NameEn)[] SeedBodies =
    {
        ("sdaia_ndmo", "سدايا/المكتب الوطني لإدارة البيانات", "SDAIA/NDMO"),
        ("nca", "الهيئة الوطنية للأمن السيبراني", "NCA"),
        ("dga", "هيئة الحكومة الرقمية", "DGA"),
        ("cst", "هيئة الاتصالات والفضاء والتقنية", "CST"),
        ("rdia", "هيئة البحث والتطوير والابتكار", "RDIA"),
    };

    public void Configure(EntityTypeBuilder<StandardBody> builder)
    {
        builder.ToTable("StandardBodies");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(b => b.Code).IsUnique();
        builder.Property(b => b.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(b => b.NameEn).IsRequired().HasMaxLength(200);

        builder.HasData(SeedBodies.Select((b, index) => new StandardBody
        {
            Id = new Guid($"00000000-0000-0000-0013-{(index + 1):D12}"),
            Code = b.Code,
            NameAr = b.NameAr,
            NameEn = b.NameEn,
            SortOrder = index + 1,
        }));
    }
}
