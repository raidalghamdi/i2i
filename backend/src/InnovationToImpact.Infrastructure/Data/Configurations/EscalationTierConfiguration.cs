using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class EscalationTierConfiguration : IEntityTypeConfiguration<EscalationTier>
{
    private static readonly (string Code, string NameAr, string NameEn)[] SeedTiers =
    {
        ("manager", "المدير المباشر", "Manager"),
        ("director", "المدير العام", "Director"),
        ("exec", "الإدارة التنفيذية", "Executive"),
    };

    public void Configure(EntityTypeBuilder<EscalationTier> builder)
    {
        builder.ToTable("EscalationTiers");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(t => t.Code).IsUnique();
        builder.Property(t => t.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(t => t.NameEn).IsRequired().HasMaxLength(200);

        builder.HasData(SeedTiers.Select((t, index) => new EscalationTier
        {
            Id = new Guid($"00000000-0000-0000-0019-{(index + 1):D12}"),
            Code = t.Code,
            NameAr = t.NameAr,
            NameEn = t.NameEn,
            SortOrder = index + 1,
        }));
    }
}
