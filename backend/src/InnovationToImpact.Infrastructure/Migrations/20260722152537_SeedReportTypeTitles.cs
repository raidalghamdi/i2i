using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedReportTypeTitles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ReportTitles",
                columns: new[] { "Id", "Key", "SortOrder", "TitleAr", "TitleEn" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0024-000000000006"), "executive", 6, "التقرير التنفيذي", "Executive Performance Overview" },
                    { new Guid("00000000-0000-0000-0024-000000000007"), "detailed", 7, "التقرير التفصيلي الشامل", "Comprehensive Detailed Report" },
                    { new Guid("00000000-0000-0000-0024-000000000008"), "media", 8, "تقرير الإعلام والاتصال المؤسسي", "Media & Corporate Communications Report" },
                    { new Guid("00000000-0000-0000-0024-000000000009"), "cx", 9, "تقرير تجربة المُبتكِر", "Innovator Experience Report" },
                    { new Guid("00000000-0000-0000-0024-000000000010"), "operational", 10, "التقرير التشغيلي", "Operational Performance Report" },
                    { new Guid("00000000-0000-0000-0024-000000000011"), "audit", 11, "تقرير المراجعة والامتثال", "Audit & Compliance Report" },
                    { new Guid("00000000-0000-0000-0024-000000000012"), "ideas", 12, "سجل الأفكار", "Ideas Register" },
                    { new Guid("00000000-0000-0000-0024-000000000013"), "evaluators", 13, "تقرير أداء المُقيّمين", "Evaluator Performance Report" },
                    { new Guid("00000000-0000-0000-0024-000000000014"), "themes", 14, "تقرير المسارات الاستراتيجية", "Strategic Themes Report" },
                    { new Guid("00000000-0000-0000-0024-000000000015"), "innovators", 15, "تقرير المُبتكِرين", "Innovators Report" },
                    { new Guid("00000000-0000-0000-0024-000000000016"), "committee", 16, "تقرير قرارات اللجنة", "Committee Decisions Report" },
                    { new Guid("00000000-0000-0000-0024-000000000017"), "trends", 17, "تقرير الاتجاهات والتحليل الزمني", "Trends & Time-Series Analysis" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ReportTitles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0024-000000000006"));

            migrationBuilder.DeleteData(
                table: "ReportTitles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0024-000000000007"));

            migrationBuilder.DeleteData(
                table: "ReportTitles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0024-000000000008"));

            migrationBuilder.DeleteData(
                table: "ReportTitles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0024-000000000009"));

            migrationBuilder.DeleteData(
                table: "ReportTitles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0024-000000000010"));

            migrationBuilder.DeleteData(
                table: "ReportTitles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0024-000000000011"));

            migrationBuilder.DeleteData(
                table: "ReportTitles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0024-000000000012"));

            migrationBuilder.DeleteData(
                table: "ReportTitles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0024-000000000013"));

            migrationBuilder.DeleteData(
                table: "ReportTitles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0024-000000000014"));

            migrationBuilder.DeleteData(
                table: "ReportTitles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0024-000000000015"));

            migrationBuilder.DeleteData(
                table: "ReportTitles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0024-000000000016"));

            migrationBuilder.DeleteData(
                table: "ReportTitles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0024-000000000017"));
        }
    }
}
