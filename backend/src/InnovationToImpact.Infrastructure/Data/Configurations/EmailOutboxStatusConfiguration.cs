using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class EmailOutboxStatusConfiguration : IEntityTypeConfiguration<EmailOutboxStatus>
{
    private static readonly (string Code, string NameAr, string NameEn)[] SeedStatuses =
    {
        ("pending", "قيد الانتظار", "Pending"),
        ("sending", "قيد الإرسال", "Sending"),
        ("sent", "مرسل", "Sent"),
        ("failed", "فشل", "Failed"),
        ("skipped", "تم التخطي", "Skipped"),
    };

    public void Configure(EntityTypeBuilder<EmailOutboxStatus> builder)
    {
        builder.ToTable("EmailOutboxStatuses");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(s => s.Code).IsUnique();
        builder.Property(s => s.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(s => s.NameEn).IsRequired().HasMaxLength(200);

        builder.HasData(SeedStatuses.Select((s, index) => new EmailOutboxStatus
        {
            Id = new Guid($"00000000-0000-0000-0017-{(index + 1):D12}"),
            Code = s.Code,
            NameAr = s.NameAr,
            NameEn = s.NameEn,
            SortOrder = index + 1,
        }));
    }
}
