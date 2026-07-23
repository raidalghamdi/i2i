using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class PhaseScheduleConfiguration : IEntityTypeConfiguration<PhaseSchedule>
{
    private static readonly DateTime SeedUpdatedAt = new(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<PhaseSchedule> builder)
    {
        builder.ToTable("PhaseSchedules");
        builder.HasKey(p => p.Idx);
        builder.Property(p => p.Idx).ValueGeneratedNever();
        builder.Property(p => p.Code).IsRequired().HasMaxLength(50);
        builder.Property(p => p.LabelAr).IsRequired().HasMaxLength(200);
        builder.Property(p => p.LabelEn).IsRequired().HasMaxLength(200);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.UpdatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new PhaseSchedule { Idx = 0, Code = "submission", LabelAr = "تقديم الأفكار", LabelEn = "Idea Submission", UpdatedAt = SeedUpdatedAt },
            new PhaseSchedule { Idx = 1, Code = "screening", LabelAr = "الفرز", LabelEn = "Screening", UpdatedAt = SeedUpdatedAt },
            new PhaseSchedule { Idx = 2, Code = "evaluation", LabelAr = "التقييم", LabelEn = "Evaluation", UpdatedAt = SeedUpdatedAt },
            new PhaseSchedule { Idx = 3, Code = "committee", LabelAr = "مراجعة اللجنة", LabelEn = "Committee Review", UpdatedAt = SeedUpdatedAt },
            new PhaseSchedule { Idx = 4, Code = "pilot", LabelAr = "التجريب", LabelEn = "Pilot", UpdatedAt = SeedUpdatedAt },
            new PhaseSchedule { Idx = 5, Code = "implementation", LabelAr = "التنفيذ", LabelEn = "Implementation", UpdatedAt = SeedUpdatedAt },
            new PhaseSchedule { Idx = 6, Code = "benefits_tracking", LabelAr = "تتبع الفوائد", LabelEn = "Benefits Tracking", UpdatedAt = SeedUpdatedAt });
    }
}
