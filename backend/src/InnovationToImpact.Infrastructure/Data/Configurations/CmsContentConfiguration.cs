using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class CmsContentConfiguration : IEntityTypeConfiguration<CmsContent>
{
    public void Configure(EntityTypeBuilder<CmsContent> builder)
    {
        builder.ToTable("CmsContents");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Slug).IsRequired().HasMaxLength(150);
        builder.HasIndex(c => c.Slug).IsUnique();

        builder.Property(c => c.TitleAr).IsRequired().HasMaxLength(300);
        builder.Property(c => c.TitleEn).IsRequired().HasMaxLength(300);
        builder.Property(c => c.BodyAr).IsRequired();
        builder.Property(c => c.BodyEn).IsRequired();

        builder.Property(c => c.IsPublished).HasDefaultValue(true);

        var seededAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(new CmsContent
        {
            Id = new Guid("00000000-0000-0000-0034-000000000001"),
            Slug = "terms",
            TitleAr = "الشروط والأحكام",
            TitleEn = "Terms & Conditions",
            BodyAr = string.Join("\n\n", new[]
            {
                "باستخدامك منصة «من الابتكار إلى الأثر» فإنك توافق على هذه الشروط والأحكام.",
                "أنت مسؤول عن دقة المعلومات التي تقدّمها وعن امتلاكك الحق في مشاركتها.",
                "تخضع الأفكار المقدَّمة لشروط الملكية الفكرية التي تُقرّها عند التقديم.",
                "يحق للهيئة قبول أي فكرة أو طلب تعديلها أو رفضها أو إحالتها وفق معايير التقييم المنشورة.",
                "تُقدَّم المنصة «كما هي»، ويجوز للهيئة تحديث هذه الشروط وتفاصيل البرنامج من حين لآخر.",
            }),
            BodyEn = string.Join("\n\n", new[]
            {
                "By using the Innovation to Impact platform you agree to these Terms & Conditions.",
                "You are responsible for the accuracy of the information you submit and for ensuring you have the right to share it.",
                "Submitted ideas are subject to the intellectual-property terms acknowledged at submission time.",
                "The Authority may accept, request revision of, reject, or escalate any submission at its discretion based on the published evaluation criteria.",
                "The platform is provided on an \"as is\" basis; the Authority may update these terms and program details from time to time.",
            }),
            IsPublished = true,
            PublishedAt = seededAt,
            UpdatedAt = seededAt,
        });
    }
}
