using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedComplianceControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ComplianceControls",
                columns: new[] { "Id", "ComplianceControlStatusId", "ControlCode", "DescriptionAr", "DescriptionEn", "EvidenceUrlsJson", "LastReviewedAt", "MappedFeaturePathsJson", "OwnerId", "StandardBodyId", "TitleAr", "TitleEn" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0033-000000000001"), new Guid("00000000-0000-0000-0014-000000000003"), "NDMO-DG-01", "توثيق سياسة حوكمة البيانات المؤسسية وأدوار ومسؤوليات ملاك البيانات وفق إطار المكتب الوطني لإدارة البيانات.", "Documented enterprise data governance policy defining data owner roles and responsibilities per the National Data Management Office framework.", null, null, "[\"/admin/audit\",\"/admin/reports\"]", null, new Guid("00000000-0000-0000-0013-000000000001"), "سياسة حوكمة البيانات", "Data Governance Policy" },
                    { new Guid("00000000-0000-0000-0033-000000000002"), new Guid("00000000-0000-0000-0014-000000000002"), "NDMO-DG-02", "تصنيف مجموعات البيانات حسب الحساسية وتطبيق ضوابط المعالجة والاحتفاظ المناسبة لكل مستوى تصنيف.", "Data sets classified by sensitivity with handling and retention controls applied per classification level.", null, null, "[\"/admin/users\",\"/admin/audit\"]", null, new Guid("00000000-0000-0000-0013-000000000001"), "تصنيف البيانات ومعالجتها", "Data Classification & Handling" },
                    { new Guid("00000000-0000-0000-0033-000000000003"), new Guid("00000000-0000-0000-0014-000000000003"), "NCA-ECC-01", "تطبيق ضوابط التحكم بالوصول المبنية على الأدوار والمصادقة متعددة العوامل وفق الضوابط الأساسية للأمن السيبراني.", "Role-based access control and multi-factor authentication implemented per the Essential Cybersecurity Controls.", null, null, "[\"/login\",\"/admin/users\",\"/admin/roster\"]", null, new Guid("00000000-0000-0000-0013-000000000002"), "إدارة الهوية والتحكم بالوصول", "Access Control & Identity Management" },
                    { new Guid("00000000-0000-0000-0033-000000000004"), new Guid("00000000-0000-0000-0014-000000000002"), "NCA-ECC-02", "تفعيل سجل تدقيق غير قابل للتعديل لجميع العمليات الحساسة مع مراقبة مستمرة للأحداث الأمنية.", "Tamper-evident audit logging enabled for all sensitive operations with continuous security event monitoring.", null, null, "[\"/admin/audit\",\"/admin/audit/verify\"]", null, new Guid("00000000-0000-0000-0013-000000000002"), "تسجيل الأحداث ومراقبة الأمن", "Security Logging & Monitoring" },
                    { new Guid("00000000-0000-0000-0033-000000000005"), new Guid("00000000-0000-0000-0014-000000000001"), "NCA-ECC-03", "إعداد واعتماد خطة استجابة للحوادث الأمنية تشمل التصعيد والتبليغ وفق متطلبات الهيئة الوطنية للأمن السيبراني.", "A documented, approved incident response plan covering escalation and reporting per NCA requirements.", null, null, "[\"/admin/escalations\"]", null, new Guid("00000000-0000-0000-0013-000000000002"), "خطة الاستجابة للحوادث", "Incident Response Plan" },
                    { new Guid("00000000-0000-0000-0033-000000000006"), new Guid("00000000-0000-0000-0014-000000000003"), "DGA-OSS-01", "استخدام معايير بيانات مفتوحة وواجهات برمجية موثقة لضمان قابلية التشغيل البيني مع الأنظمة الحكومية الأخرى.", "Open data standards and documented APIs used to ensure interoperability with other government systems.", null, null, "[\"/admin/analytics\",\"/admin/reports\"]", null, new Guid("00000000-0000-0000-0013-000000000003"), "المعايير المفتوحة وقابلية التشغيل البيني", "Open Standards & Interoperability" },
                    { new Guid("00000000-0000-0000-0033-000000000007"), new Guid("00000000-0000-0000-0014-000000000002"), "WCAG-2.1-AA", "تلبية الخدمات الرقمية لمتطلبات إرشادات إمكانية الوصول لمحتوى الويب على المستوى AA وفق معايير هيئة الحكومة الرقمية.", "Digital services meet Web Content Accessibility Guidelines (WCAG) 2.1 Level AA per Digital Government Authority standards.", null, null, "[\"/ideas\",\"/idea/submit\",\"/profile\"]", null, new Guid("00000000-0000-0000-0013-000000000003"), "الامتثال لإمكانية الوصول الرقمي", "Digital Accessibility Compliance" },
                    { new Guid("00000000-0000-0000-0033-000000000008"), new Guid("00000000-0000-0000-0014-000000000003"), "CST-DP-01", "استضافة البيانات ضمن مراكز بيانات معتمدة داخل المملكة وفق ضوابط هيئة الاتصالات والفضاء والتقنية.", "Data hosted within Kingdom-approved data centers per Communications, Space & Technology Commission (CST) cloud regulations.", null, null, "[\"/admin/reports\"]", null, new Guid("00000000-0000-0000-0013-000000000004"), "توطين البيانات والامتثال السحابي", "Data Localization & Cloud Compliance" },
                    { new Guid("00000000-0000-0000-0033-000000000009"), new Guid("00000000-0000-0000-0014-000000000001"), "CST-DP-02", "تطبيق ضوابط حماية بيانات المستخدمين المتعلقة بالاتصالات وفق لوائح هيئة الاتصالات والفضاء والتقنية.", "Telecom-related user data protection controls applied per CST regulations.", null, null, "[\"/admin/email-log\",\"/admin/support\"]", null, new Guid("00000000-0000-0000-0013-000000000004"), "حماية بيانات الاتصالات", "Telecom Data Protection" },
                    { new Guid("00000000-0000-0000-0033-000000000010"), new Guid("00000000-0000-0000-0014-000000000003"), "RDIA-INV-01", "إصدار تقارير دورية توثق أثر برامج الابتكار ومؤشرات الأداء وفق متطلبات هيئة البحث والتطوير والابتكار.", "Periodic reports documenting innovation program impact and KPIs per Research, Development & Innovation Authority (RDIA) requirements.", null, null, "[\"/admin/analytics\",\"/admin/reports\"]", null, new Guid("00000000-0000-0000-0013-000000000005"), "تقارير أثر برامج الابتكار", "Innovation Program Impact Reporting" },
                    { new Guid("00000000-0000-0000-0033-000000000011"), new Guid("00000000-0000-0000-0014-000000000001"), "RDIA-INV-02", "تتبع مؤشرات أداء البحث والتطوير على مستوى الأفكار والمبادرات المقدمة عبر المنصة.", "Tracking of R&D performance metrics across ideas and initiatives submitted through the platform.", null, null, "[\"/admin/analytics\"]", null, new Guid("00000000-0000-0000-0013-000000000005"), "تتبع مؤشرات البحث والتطوير", "R&D Metrics & KPI Tracking" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ComplianceControls",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0033-000000000001"));

            migrationBuilder.DeleteData(
                table: "ComplianceControls",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0033-000000000002"));

            migrationBuilder.DeleteData(
                table: "ComplianceControls",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0033-000000000003"));

            migrationBuilder.DeleteData(
                table: "ComplianceControls",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0033-000000000004"));

            migrationBuilder.DeleteData(
                table: "ComplianceControls",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0033-000000000005"));

            migrationBuilder.DeleteData(
                table: "ComplianceControls",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0033-000000000006"));

            migrationBuilder.DeleteData(
                table: "ComplianceControls",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0033-000000000007"));

            migrationBuilder.DeleteData(
                table: "ComplianceControls",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0033-000000000008"));

            migrationBuilder.DeleteData(
                table: "ComplianceControls",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0033-000000000009"));

            migrationBuilder.DeleteData(
                table: "ComplianceControls",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0033-000000000010"));

            migrationBuilder.DeleteData(
                table: "ComplianceControls",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0033-000000000011"));
        }
    }
}
