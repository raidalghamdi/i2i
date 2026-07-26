using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Roster;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class RoleInvitationStatusConfiguration : IEntityTypeConfiguration<RoleInvitationStatus>
{
    private static readonly (string Code, string NameAr, string NameEn)[] SeedStatuses =
    {
        (RoleInvitationStatusCodes.Pending, "قيد الانتظار", "Pending"),
        (RoleInvitationStatusCodes.Applied, "مطبّقة", "Applied"),
        (RoleInvitationStatusCodes.Expired, "منتهية الصلاحية", "Expired"),
        (RoleInvitationStatusCodes.Withdrawn, "مسحوبة", "Withdrawn"),
    };

    public void Configure(EntityTypeBuilder<RoleInvitationStatus> builder)
    {
        builder.ToTable("RoleInvitationStatuses");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(s => s.Code).IsUnique();
        builder.Property(s => s.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(s => s.NameEn).IsRequired().HasMaxLength(200);

        builder.HasData(SeedStatuses.Select((s, index) => new RoleInvitationStatus
        {
            Id = new Guid($"00000000-0000-0000-0030-{(index + 1):D12}"),
            Code = s.Code,
            NameAr = s.NameAr,
            NameEn = s.NameEn,
            SortOrder = index + 1,
        }));
    }
}
