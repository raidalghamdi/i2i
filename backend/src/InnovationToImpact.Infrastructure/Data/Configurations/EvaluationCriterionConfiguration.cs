using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

// Change 20260726
public class EvaluationCriterionConfiguration : IEntityTypeConfiguration<EvaluationCriterion>
{
    // Codes must match the values already persisted in Evaluations.CriteriaScoresJson, otherwise
    // historical scores stop resolving to a criterion.
    private static readonly (string Code, string NameAr, string NameEn, decimal Weight)[] SeedCriteria =
    {
        ("innovation", "الابتكار", "Innovation", 0.20m),
        ("impact", "الأثر", "Impact", 0.20m),
        ("execution", "قابلية التنفيذ", "Execution", 0.20m),
        ("scalability", "قابلية التوسع", "Scalability", 0.20m),
        ("presentation", "العرض والتقديم", "Presentation", 0.20m),
    };

    public void Configure(EntityTypeBuilder<EvaluationCriterion> builder)
    {
        builder.ToTable("EvaluationCriteria");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(c => c.Code).IsUnique();
        builder.Property(c => c.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(c => c.NameEn).IsRequired().HasMaxLength(200);
        builder.Property(c => c.DescriptionAr).HasMaxLength(1000);
        builder.Property(c => c.DescriptionEn).HasMaxLength(1000);
        builder.Property(c => c.Weight).HasPrecision(5, 2);

        builder.HasData(SeedCriteria.Select((c, index) => new EvaluationCriterion
        {
            Id = new Guid($"00000000-0000-0000-0031-{(index + 1):D12}"),
            Code = c.Code,
            NameAr = c.NameAr,
            NameEn = c.NameEn,
            Weight = c.Weight,
            Active = true,
            SortOrder = index + 1,
        }));
    }
}
