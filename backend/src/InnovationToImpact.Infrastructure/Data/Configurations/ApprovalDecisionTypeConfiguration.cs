using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class ApprovalDecisionTypeConfiguration : IEntityTypeConfiguration<ApprovalDecisionType>
{
    private static readonly (string Code, string NameAr, string NameEn)[] SeedTypes =
    {
        ("approve", "موافقة", "Approve"),
        ("reject", "رفض", "Reject"),
        ("request_changes", "طلب تعديلات", "Request Changes"),
    };

    public void Configure(EntityTypeBuilder<ApprovalDecisionType> builder)
    {
        builder.ToTable("ApprovalDecisionTypes");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(d => d.Code).IsUnique();
        builder.Property(d => d.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(d => d.NameEn).IsRequired().HasMaxLength(200);

        builder.HasData(SeedTypes.Select((t, index) => new ApprovalDecisionType
        {
            Id = new Guid($"00000000-0000-0000-0022-{(index + 1):D12}"),
            Code = t.Code,
            NameAr = t.NameAr,
            NameEn = t.NameEn,
            SortOrder = index + 1,
        }));
    }
}
