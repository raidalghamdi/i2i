using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class PilotStatusConfiguration : IEntityTypeConfiguration<PilotStatus>
{
    private static readonly (string Code, string NameAr, string NameEn)[] SeedStatuses =
    {
        ("planned", "مخطط له", "Planned"),
        ("in_progress", "قيد التنفيذ", "In Progress"),
        ("completed", "مكتمل", "Completed"),
        ("cancelled", "ملغى", "Cancelled"),
    };

    public void Configure(EntityTypeBuilder<PilotStatus> builder)
    {
        builder.ToTable("PilotStatuses");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(s => s.Code).IsUnique();
        builder.Property(s => s.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(s => s.NameEn).IsRequired().HasMaxLength(200);

        builder.HasData(SeedStatuses.Select((s, index) => new PilotStatus
        {
            Id = new Guid($"00000000-0000-0000-0004-{(index + 1):D12}"),
            Code = s.Code,
            NameAr = s.NameAr,
            NameEn = s.NameEn,
            SortOrder = index + 1,
        }));
    }
}
