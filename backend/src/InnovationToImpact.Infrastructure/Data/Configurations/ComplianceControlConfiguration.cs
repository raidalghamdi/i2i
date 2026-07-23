using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class ComplianceControlConfiguration : IEntityTypeConfiguration<ComplianceControl>
{
    // StandardBodyIndex: 1=sdaia_ndmo, 2=nca, 3=dga, 4=cst, 5=rdia (see StandardBodyConfiguration).
    // StatusIndex: 1=not_started, 2=in_progress, 3=met, 4=not_applicable (see ComplianceControlStatusConfiguration).
    private static readonly (string ControlCode, int StandardBodyIndex, string TitleAr, string TitleEn, string DescriptionAr, string DescriptionEn, string MappedFeaturePathsJson, int StatusIndex)[] SeedControls =
    {
        (
            "NDMO-DG-01", 1,
            "سياسة حوكمة البيانات", "Data Governance Policy",
            "توثيق سياسة حوكمة البيانات المؤسسية وأدوار ومسؤوليات ملاك البيانات وفق إطار المكتب الوطني لإدارة البيانات.",
            "Documented enterprise data governance policy defining data owner roles and responsibilities per the National Data Management Office framework.",
            "[\"/admin/audit\",\"/admin/reports\"]", 3
        ),
        (
            "NDMO-DG-02", 1,
            "تصنيف البيانات ومعالجتها", "Data Classification & Handling",
            "تصنيف مجموعات البيانات حسب الحساسية وتطبيق ضوابط المعالجة والاحتفاظ المناسبة لكل مستوى تصنيف.",
            "Data sets classified by sensitivity with handling and retention controls applied per classification level.",
            "[\"/admin/users\",\"/admin/audit\"]", 2
        ),
        (
            "NCA-ECC-01", 2,
            "إدارة الهوية والتحكم بالوصول", "Access Control & Identity Management",
            "تطبيق ضوابط التحكم بالوصول المبنية على الأدوار والمصادقة متعددة العوامل وفق الضوابط الأساسية للأمن السيبراني.",
            "Role-based access control and multi-factor authentication implemented per the Essential Cybersecurity Controls.",
            "[\"/login\",\"/admin/users\",\"/admin/roster\"]", 3
        ),
        (
            "NCA-ECC-02", 2,
            "تسجيل الأحداث ومراقبة الأمن", "Security Logging & Monitoring",
            "تفعيل سجل تدقيق غير قابل للتعديل لجميع العمليات الحساسة مع مراقبة مستمرة للأحداث الأمنية.",
            "Tamper-evident audit logging enabled for all sensitive operations with continuous security event monitoring.",
            "[\"/admin/audit\",\"/admin/audit/verify\"]", 2
        ),
        (
            "NCA-ECC-03", 2,
            "خطة الاستجابة للحوادث", "Incident Response Plan",
            "إعداد واعتماد خطة استجابة للحوادث الأمنية تشمل التصعيد والتبليغ وفق متطلبات الهيئة الوطنية للأمن السيبراني.",
            "A documented, approved incident response plan covering escalation and reporting per NCA requirements.",
            "[\"/admin/escalations\"]", 1
        ),
        (
            "DGA-OSS-01", 3,
            "المعايير المفتوحة وقابلية التشغيل البيني", "Open Standards & Interoperability",
            "استخدام معايير بيانات مفتوحة وواجهات برمجية موثقة لضمان قابلية التشغيل البيني مع الأنظمة الحكومية الأخرى.",
            "Open data standards and documented APIs used to ensure interoperability with other government systems.",
            "[\"/admin/analytics\",\"/admin/reports\"]", 3
        ),
        (
            "WCAG-2.1-AA", 3,
            "الامتثال لإمكانية الوصول الرقمي", "Digital Accessibility Compliance",
            "تلبية الخدمات الرقمية لمتطلبات إرشادات إمكانية الوصول لمحتوى الويب على المستوى AA وفق معايير هيئة الحكومة الرقمية.",
            "Digital services meet Web Content Accessibility Guidelines (WCAG) 2.1 Level AA per Digital Government Authority standards.",
            "[\"/ideas\",\"/idea/submit\",\"/profile\"]", 2
        ),
        (
            "CST-DP-01", 4,
            "توطين البيانات والامتثال السحابي", "Data Localization & Cloud Compliance",
            "استضافة البيانات ضمن مراكز بيانات معتمدة داخل المملكة وفق ضوابط هيئة الاتصالات والفضاء والتقنية.",
            "Data hosted within Kingdom-approved data centers per Communications, Space & Technology Commission (CST) cloud regulations.",
            "[\"/admin/reports\"]", 3
        ),
        (
            "CST-DP-02", 4,
            "حماية بيانات الاتصالات", "Telecom Data Protection",
            "تطبيق ضوابط حماية بيانات المستخدمين المتعلقة بالاتصالات وفق لوائح هيئة الاتصالات والفضاء والتقنية.",
            "Telecom-related user data protection controls applied per CST regulations.",
            "[\"/admin/email-log\",\"/admin/support\"]", 1
        ),
        (
            "RDIA-INV-01", 5,
            "تقارير أثر برامج الابتكار", "Innovation Program Impact Reporting",
            "إصدار تقارير دورية توثق أثر برامج الابتكار ومؤشرات الأداء وفق متطلبات هيئة البحث والتطوير والابتكار.",
            "Periodic reports documenting innovation program impact and KPIs per Research, Development & Innovation Authority (RDIA) requirements.",
            "[\"/admin/analytics\",\"/admin/reports\"]", 3
        ),
        (
            "RDIA-INV-02", 5,
            "تتبع مؤشرات البحث والتطوير", "R&D Metrics & KPI Tracking",
            "تتبع مؤشرات أداء البحث والتطوير على مستوى الأفكار والمبادرات المقدمة عبر المنصة.",
            "Tracking of R&D performance metrics across ideas and initiatives submitted through the platform.",
            "[\"/admin/analytics\"]", 1
        ),
    };

    public void Configure(EntityTypeBuilder<ComplianceControl> builder)
    {
        builder.ToTable("ComplianceControls");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.ControlCode).IsRequired().HasMaxLength(100);
        builder.HasIndex(c => c.ControlCode).IsUnique();

        builder.Property(c => c.TitleAr).IsRequired().HasMaxLength(500);
        builder.Property(c => c.TitleEn).IsRequired().HasMaxLength(500);
        builder.Property(c => c.DescriptionAr).IsRequired();
        builder.Property(c => c.DescriptionEn).IsRequired();

        builder.HasOne(c => c.StandardBody)
            .WithMany()
            .HasForeignKey(c => c.StandardBodyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Owner)
            .WithMany()
            .HasForeignKey(c => c.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ComplianceControlStatus)
            .WithMany()
            .HasForeignKey(c => c.ComplianceControlStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(SeedControls.Select((c, index) => new ComplianceControl
        {
            Id = new Guid($"00000000-0000-0000-0033-{(index + 1):D12}"),
            ControlCode = c.ControlCode,
            StandardBodyId = new Guid($"00000000-0000-0000-0013-{c.StandardBodyIndex:D12}"),
            TitleAr = c.TitleAr,
            TitleEn = c.TitleEn,
            DescriptionAr = c.DescriptionAr,
            DescriptionEn = c.DescriptionEn,
            MappedFeaturePathsJson = c.MappedFeaturePathsJson,
            EvidenceUrlsJson = null,
            OwnerId = null,
            ComplianceControlStatusId = new Guid($"00000000-0000-0000-0014-{c.StatusIndex:D12}"),
            LastReviewedAt = null,
        }));
    }
}
