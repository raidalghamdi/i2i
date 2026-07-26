using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class HandoverStatusConfiguration : IEntityTypeConfiguration<HandoverStatus>
{
    private static readonly (string Code, string NameAr, string NameEn)[] SeedStatuses =
    {
        ("pending", "قيد الانتظار", "Pending"),
        ("in_progress", "قيد التنفيذ", "In Progress"),
        ("completed", "مكتمل", "Completed"),
    };

    public void Configure(EntityTypeBuilder<HandoverStatus> builder)
    {
        builder.ToTable("HandoverStatuses");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(s => s.Code).IsUnique();
        builder.Property(s => s.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(s => s.NameEn).IsRequired().HasMaxLength(200);

        builder.HasData(SeedStatuses.Select((s, index) => new HandoverStatus
        {
            Id = new Guid($"00000000-0000-0000-0009-{(index + 1):D12}"),
            Code = s.Code,
            NameAr = s.NameAr,
            NameEn = s.NameEn,
            SortOrder = index + 1,
        }));
    }
}
