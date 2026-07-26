using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class TeamInvitationStatusConfiguration : IEntityTypeConfiguration<TeamInvitationStatus>
{
    private static readonly (string Code, string NameAr, string NameEn)[] SeedStatuses =
    {
        ("pending", "قيد الانتظار", "Pending"),
        ("accepted", "مقبولة", "Accepted"),
        ("declined", "مرفوضة", "Declined"),
        ("expired", "منتهية الصلاحية", "Expired"),
    };

    public void Configure(EntityTypeBuilder<TeamInvitationStatus> builder)
    {
        builder.ToTable("TeamInvitationStatuses");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(s => s.Code).IsUnique();
        builder.Property(s => s.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(s => s.NameEn).IsRequired().HasMaxLength(200);

        builder.HasData(SeedStatuses.Select((s, index) => new TeamInvitationStatus
        {
            Id = new Guid($"00000000-0000-0000-0016-{(index + 1):D12}"),
            Code = s.Code,
            NameAr = s.NameAr,
            NameEn = s.NameEn,
            SortOrder = index + 1,
        }));
    }
}
