using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class CommitteeCriterionConfiguration : IEntityTypeConfiguration<CommitteeCriterion>
{
    private static readonly (string Code, string NameAr, string NameEn, decimal Weight)[] SeedCriteria =
    {
        ("originality", "الأصالة", "Originality", 0.30m),
        ("feasibility", "قابلية التنفيذ", "Feasibility", 0.25m),
        ("impact", "الأثر", "Impact", 0.30m),
        ("alignment", "التوافق الاستراتيجي", "Strategic Alignment", 0.15m),
    };

    public void Configure(EntityTypeBuilder<CommitteeCriterion> builder)
    {
        builder.ToTable("CommitteeCriteria");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(c => c.Code).IsUnique();
        builder.Property(c => c.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(c => c.NameEn).IsRequired().HasMaxLength(200);
        builder.Property(c => c.DescriptionAr).HasMaxLength(1000);
        builder.Property(c => c.DescriptionEn).HasMaxLength(1000);
        builder.Property(c => c.Weight).HasPrecision(5, 2);

        builder.HasData(SeedCriteria.Select((c, index) => new CommitteeCriterion
        {
            Id = new Guid($"00000000-0000-0000-0003-{(index + 1):D12}"),
            Code = c.Code,
            NameAr = c.NameAr,
            NameEn = c.NameEn,
            Weight = c.Weight,
            Active = true,
        }));
    }
}
