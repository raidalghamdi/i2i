using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class IdeaStatusConfiguration : IEntityTypeConfiguration<IdeaStatus>
{
    private static readonly (string Code, string NameAr, string NameEn)[] SeedStatuses =
    {
        ("draft", "مسودة", "Draft"),
        ("submitted", "مُقدَّمة", "Submitted"),
        ("screening", "قيد الفرز", "Screening"),
        ("needs_completion", "بحاجة لإكمال", "Needs Completion"),
        ("evaluation", "قيد التقييم", "Evaluation"),
        ("committee", "قيد اللجنة", "Committee"),
        ("approved", "معتمدة", "Approved"),
        ("rejected", "مرفوضة", "Rejected"),
        ("returned", "معادة", "Returned"),
        ("assigned", "مسندة", "Assigned"),
        ("in_pilot", "قيد التجريب", "In Pilot"),
        ("in_implementation", "قيد التنفيذ", "In Implementation"),
        ("benefits_tracking", "تتبع المنافع", "Benefits Tracking"),
        ("closed", "مغلقة", "Closed"),
        ("archived", "مؤرشفة", "Archived"),
        ("withdrawn", "مسحوبة", "Withdrawn"),
        ("pass_awaiting_attachments", "بانتظار المرفقات", "Pass Awaiting Attachments"),
        ("pending_final_ranking", "بانتظار الترتيب النهائي", "Pending Final Ranking"),
        ("evaluation_failed", "فشل التقييم", "Evaluation Failed"),
        ("not_selected", "لم يتم اختيارها", "Not Selected"),
        ("in_measurement", "قيد القياس", "In Measurement"),
        ("in_scaling", "قيد التوسّع", "In Scaling"),
    };

    public void Configure(EntityTypeBuilder<IdeaStatus> builder)
    {
        builder.ToTable("IdeaStatuses");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(s => s.Code).IsUnique();
        builder.Property(s => s.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(s => s.NameEn).IsRequired().HasMaxLength(200);

        builder.HasData(SeedStatuses.Select((s, index) => new IdeaStatus
        {
            Id = new Guid($"00000000-0000-0000-0000-{(index + 1):D12}"),
            Code = s.Code,
            NameAr = s.NameAr,
            NameEn = s.NameEn,
            SortOrder = index + 1,
        }));
    }
}
