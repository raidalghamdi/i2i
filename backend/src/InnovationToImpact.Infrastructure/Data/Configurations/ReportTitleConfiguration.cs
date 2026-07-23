using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class ReportTitleConfiguration : IEntityTypeConfiguration<ReportTitle>
{
    public void Configure(EntityTypeBuilder<ReportTitle> builder)
    {
        builder.ToTable("ReportTitles");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Key).IsRequired().HasMaxLength(150);
        builder.HasIndex(r => r.Key).IsUnique();

        builder.Property(r => r.TitleAr).IsRequired().HasMaxLength(300);
        builder.Property(r => r.TitleEn).IsRequired().HasMaxLength(300);

        builder.HasData(
            new ReportTitle
            {
                Id = new Guid("00000000-0000-0000-0024-000000000001"),
                Key = "audit_log_export",
                TitleAr = "تصدير سجل التدقيق",
                TitleEn = "Audit Log Export",
                SortOrder = 1,
            },
            new ReportTitle
            {
                Id = new Guid("00000000-0000-0000-0024-000000000002"),
                Key = "ideas_export",
                TitleAr = "تصدير الأفكار",
                TitleEn = "Ideas Export",
                SortOrder = 2,
            },
            new ReportTitle
            {
                Id = new Guid("00000000-0000-0000-0024-000000000003"),
                Key = "evaluations_export",
                TitleAr = "تصدير التقييمات",
                TitleEn = "Evaluations Export",
                SortOrder = 3,
            },
            new ReportTitle
            {
                Id = new Guid("00000000-0000-0000-0024-000000000004"),
                Key = "escalations_export",
                TitleAr = "تصدير التصعيدات",
                TitleEn = "Escalations Export",
                SortOrder = 4,
            },
            new ReportTitle
            {
                Id = new Guid("00000000-0000-0000-0024-000000000005"),
                Key = "analytics_export",
                TitleAr = "تصدير التحليلات",
                TitleEn = "Analytics Export",
                SortOrder = 5,
            },
            new ReportTitle
            {
                Id = new Guid("00000000-0000-0000-0024-000000000006"),
                Key = "executive",
                TitleAr = "التقرير التنفيذي",
                TitleEn = "Executive Performance Overview",
                SortOrder = 6,
            },
            new ReportTitle
            {
                Id = new Guid("00000000-0000-0000-0024-000000000007"),
                Key = "detailed",
                TitleAr = "التقرير التفصيلي الشامل",
                TitleEn = "Comprehensive Detailed Report",
                SortOrder = 7,
            },
            new ReportTitle
            {
                Id = new Guid("00000000-0000-0000-0024-000000000008"),
                Key = "media",
                TitleAr = "تقرير الإعلام والاتصال المؤسسي",
                TitleEn = "Media & Corporate Communications Report",
                SortOrder = 8,
            },
            new ReportTitle
            {
                Id = new Guid("00000000-0000-0000-0024-000000000009"),
                Key = "cx",
                TitleAr = "تقرير تجربة المُبتكِر",
                TitleEn = "Innovator Experience Report",
                SortOrder = 9,
            },
            new ReportTitle
            {
                Id = new Guid("00000000-0000-0000-0024-000000000010"),
                Key = "operational",
                TitleAr = "التقرير التشغيلي",
                TitleEn = "Operational Performance Report",
                SortOrder = 10,
            },
            new ReportTitle
            {
                Id = new Guid("00000000-0000-0000-0024-000000000011"),
                Key = "audit",
                TitleAr = "تقرير المراجعة والامتثال",
                TitleEn = "Audit & Compliance Report",
                SortOrder = 11,
            },
            new ReportTitle
            {
                Id = new Guid("00000000-0000-0000-0024-000000000012"),
                Key = "ideas",
                TitleAr = "سجل الأفكار",
                TitleEn = "Ideas Register",
                SortOrder = 12,
            },
            new ReportTitle
            {
                Id = new Guid("00000000-0000-0000-0024-000000000013"),
                Key = "evaluators",
                TitleAr = "تقرير أداء المُقيّمين",
                TitleEn = "Evaluator Performance Report",
                SortOrder = 13,
            },
            new ReportTitle
            {
                Id = new Guid("00000000-0000-0000-0024-000000000014"),
                Key = "themes",
                TitleAr = "تقرير المسارات الاستراتيجية",
                TitleEn = "Strategic Themes Report",
                SortOrder = 14,
            },
            new ReportTitle
            {
                Id = new Guid("00000000-0000-0000-0024-000000000015"),
                Key = "innovators",
                TitleAr = "تقرير المُبتكِرين",
                TitleEn = "Innovators Report",
                SortOrder = 15,
            },
            new ReportTitle
            {
                Id = new Guid("00000000-0000-0000-0024-000000000016"),
                Key = "committee",
                TitleAr = "تقرير قرارات اللجنة",
                TitleEn = "Committee Decisions Report",
                SortOrder = 16,
            },
            new ReportTitle
            {
                Id = new Guid("00000000-0000-0000-0024-000000000017"),
                Key = "trends",
                TitleAr = "تقرير الاتجاهات والتحليل الزمني",
                TitleEn = "Trends & Time-Series Analysis",
                SortOrder = 17,
            });
    }
}
