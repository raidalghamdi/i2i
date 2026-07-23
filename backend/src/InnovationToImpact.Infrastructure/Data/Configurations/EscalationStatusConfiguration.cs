using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class EscalationStatusConfiguration : IEntityTypeConfiguration<EscalationStatus>
{
    private static readonly (string Code, string NameAr, string NameEn)[] SeedStatuses =
    {
        ("open", "مفتوح", "Open"),
        ("acknowledged", "تم الإقرار", "Acknowledged"),
        ("resolved", "تم الحل", "Resolved"),
        ("cancelled", "ملغى", "Cancelled"),
    };

    public void Configure(EntityTypeBuilder<EscalationStatus> builder)
    {
        builder.ToTable("EscalationStatuses");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(s => s.Code).IsUnique();
        builder.Property(s => s.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(s => s.NameEn).IsRequired().HasMaxLength(200);

        builder.HasData(SeedStatuses.Select((s, index) => new EscalationStatus
        {
            Id = new Guid($"00000000-0000-0000-0020-{(index + 1):D12}"),
            Code = s.Code,
            NameAr = s.NameAr,
            NameEn = s.NameEn,
            SortOrder = index + 1,
        }));
    }
}
