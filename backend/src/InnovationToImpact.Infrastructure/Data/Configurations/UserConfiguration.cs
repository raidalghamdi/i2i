using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.SamAccountName).IsRequired().HasMaxLength(100);
        builder.HasIndex(u => u.SamAccountName).IsUnique();
        builder.Property(u => u.Email).IsRequired().HasMaxLength(320);
        builder.Property(u => u.FullNameAr).IsRequired().HasMaxLength(200);
        builder.Property(u => u.FullNameEn).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Department).HasMaxLength(200);
        builder.Property(u => u.ManagerEmail).HasMaxLength(320);
        builder.Property(u => u.Title).HasMaxLength(200);
        builder.Property(u => u.PasswordHash).HasMaxLength(200);
        builder.Property(u => u.IsActive).HasDefaultValue(true);

        builder.HasOne(u => u.EscalationTier)
            .WithMany()
            .HasForeignKey(u => u.EscalationTierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(new User
        {
            Id = new Guid("00000000-0000-0000-0026-000000000001"),
            SamAccountName = "system",
            Email = "system@gac-demo.sa",
            FullNameAr = "حساب النظام",
            FullNameEn = "System Account",
            Points = 0,
            Level = 1,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
    }
}
