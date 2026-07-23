using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class StrategicThemeConfiguration : IEntityTypeConfiguration<StrategicTheme>
{
    private static readonly Guid SystemUserId = new("00000000-0000-0000-0026-000000000001");

    public void Configure(EntityTypeBuilder<StrategicTheme> builder)
    {
        builder.ToTable("StrategicThemes");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.NameAr).IsRequired().HasMaxLength(300);
        builder.Property(t => t.NameEn).IsRequired().HasMaxLength(300);
        builder.Property(t => t.DescriptionAr).HasMaxLength(2000);
        builder.Property(t => t.DescriptionEn).HasMaxLength(2000);

        builder.HasOne(t => t.Owner)
            .WithMany()
            .HasForeignKey(t => t.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new StrategicTheme
            {
                Id = new Guid("00000000-0000-0000-0025-000000000001"),
                NameAr = "التحول الرقمي",
                NameEn = "Digital Transformation",
                Priority = 1,
                OwnerId = SystemUserId,
            },
            new StrategicTheme
            {
                Id = new Guid("00000000-0000-0000-0025-000000000002"),
                NameAr = "تجربة العملاء",
                NameEn = "Customer Experience",
                Priority = 2,
                OwnerId = SystemUserId,
            },
            new StrategicTheme
            {
                Id = new Guid("00000000-0000-0000-0025-000000000003"),
                NameAr = "الكفاءة التشغيلية",
                NameEn = "Operational Efficiency",
                Priority = 3,
                OwnerId = SystemUserId,
            });
    }
}
