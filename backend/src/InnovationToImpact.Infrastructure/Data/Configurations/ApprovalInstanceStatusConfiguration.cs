using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class ApprovalInstanceStatusConfiguration : IEntityTypeConfiguration<ApprovalInstanceStatus>
{
    private static readonly (string Code, string NameAr, string NameEn)[] SeedStatuses =
    {
        ("pending", "قيد الانتظار", "Pending"),
        ("approved", "معتمد", "Approved"),
        ("rejected", "مرفوض", "Rejected"),
        ("cancelled", "ملغى", "Cancelled"),
    };

    public void Configure(EntityTypeBuilder<ApprovalInstanceStatus> builder)
    {
        builder.ToTable("ApprovalInstanceStatuses");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(s => s.Code).IsUnique();
        builder.Property(s => s.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(s => s.NameEn).IsRequired().HasMaxLength(200);

        builder.HasData(SeedStatuses.Select((s, index) => new ApprovalInstanceStatus
        {
            Id = new Guid($"00000000-0000-0000-0021-{(index + 1):D12}"),
            Code = s.Code,
            NameAr = s.NameAr,
            NameEn = s.NameEn,
            SortOrder = index + 1,
        }));
    }
}
