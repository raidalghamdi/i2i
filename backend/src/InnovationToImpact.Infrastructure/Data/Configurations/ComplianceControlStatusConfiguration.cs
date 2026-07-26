using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class ComplianceControlStatusConfiguration : IEntityTypeConfiguration<ComplianceControlStatus>
{
    private static readonly (string Code, string NameAr, string NameEn)[] SeedStatuses =
    {
        ("not_started", "لم يبدأ", "Not Started"),
        ("in_progress", "قيد التنفيذ", "In Progress"),
        ("met", "محقق", "Met"),
        ("not_applicable", "غير قابل للتطبيق", "Not Applicable"),
    };

    public void Configure(EntityTypeBuilder<ComplianceControlStatus> builder)
    {
        builder.ToTable("ComplianceControlStatuses");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(s => s.Code).IsUnique();
        builder.Property(s => s.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(s => s.NameEn).IsRequired().HasMaxLength(200);

        builder.HasData(SeedStatuses.Select((s, index) => new ComplianceControlStatus
        {
            Id = new Guid($"00000000-0000-0000-0014-{(index + 1):D12}"),
            Code = s.Code,
            NameAr = s.NameAr,
            NameEn = s.NameEn,
            SortOrder = index + 1,
        }));
    }
}
