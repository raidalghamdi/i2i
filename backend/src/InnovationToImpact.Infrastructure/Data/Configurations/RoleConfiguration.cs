using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    private static readonly (string Code, string NameAr, string NameEn)[] SeedRoles =
    {
        (RoleCodes.Admin, "مسؤول", "Admin"),
        (RoleCodes.Supervisor, "مشرف", "Supervisor"),
        (RoleCodes.Judge, "لجنة التحكيم", "Judge"),
        (RoleCodes.Evaluator, "مقيّم", "Evaluator"),
        (RoleCodes.Submitter, "مقدم الفكرة", "Submitter"),
        (RoleCodes.Expert, "خبير", "Expert"),
        (RoleCodes.Mentor, "موجه", "Mentor"),
        (RoleCodes.Facilitator, "ميسّر", "Facilitator"),
    };

    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(r => r.Code).IsUnique();
        builder.Property(r => r.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(r => r.NameEn).IsRequired().HasMaxLength(200);
        builder.Property(r => r.DescriptionAr).HasMaxLength(1000);
        builder.Property(r => r.DescriptionEn).HasMaxLength(1000);

        builder.HasData(SeedRoles.Select((r, index) => new Role
        {
            Id = new Guid($"00000000-0000-0000-0023-{(index + 1):D12}"),
            Code = r.Code,
            NameAr = r.NameAr,
            NameEn = r.NameEn,
            IsSystem = true,
            IsActive = true,
            SortOrder = index + 1,
        }));
    }
}
