using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class CommitteeDecisionTypeConfiguration : IEntityTypeConfiguration<CommitteeDecisionType>
{
    private static readonly (string Code, string NameAr, string NameEn)[] SeedTypes =
    {
        ("approved", "معتمد", "Approved"),
        ("rejected", "مرفوض", "Rejected"),
        ("deferred", "مؤجل", "Deferred"),
    };

    public void Configure(EntityTypeBuilder<CommitteeDecisionType> builder)
    {
        builder.ToTable("CommitteeDecisionTypes");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(t => t.Code).IsUnique();
        builder.Property(t => t.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(t => t.NameEn).IsRequired().HasMaxLength(200);

        builder.HasData(SeedTypes.Select((t, index) => new CommitteeDecisionType
        {
            Id = new Guid($"00000000-0000-0000-0002-{(index + 1):D12}"),
            Code = t.Code,
            NameAr = t.NameAr,
            NameEn = t.NameEn,
            SortOrder = index + 1,
        }));
    }
}
