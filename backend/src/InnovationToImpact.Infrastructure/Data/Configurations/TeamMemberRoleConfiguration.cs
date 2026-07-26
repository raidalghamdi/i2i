using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class TeamMemberRoleConfiguration : IEntityTypeConfiguration<TeamMemberRole>
{
    private static readonly (string Code, string NameAr, string NameEn)[] SeedRoles =
    {
        ("leader", "قائد", "Leader"),
        ("member", "عضو", "Member"),
    };

    public void Configure(EntityTypeBuilder<TeamMemberRole> builder)
    {
        builder.ToTable("TeamMemberRoles");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(r => r.Code).IsUnique();
        builder.Property(r => r.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(r => r.NameEn).IsRequired().HasMaxLength(200);

        builder.HasData(SeedRoles.Select((r, index) => new TeamMemberRole
        {
            Id = new Guid($"00000000-0000-0000-0015-{(index + 1):D12}"),
            Code = r.Code,
            NameAr = r.NameAr,
            NameEn = r.NameEn,
            SortOrder = index + 1,
        }));
    }
}
