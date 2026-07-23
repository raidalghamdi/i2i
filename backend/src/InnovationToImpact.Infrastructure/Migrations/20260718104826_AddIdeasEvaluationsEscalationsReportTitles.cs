using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdeasEvaluationsEscalationsReportTitles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ReportTitles",
                columns: new[] { "Id", "Key", "SortOrder", "TitleAr", "TitleEn" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0024-000000000002"), "ideas_export", 2, "تصدير الأفكار", "Ideas Export" },
                    { new Guid("00000000-0000-0000-0024-000000000003"), "evaluations_export", 3, "تصدير التقييمات", "Evaluations Export" },
                    { new Guid("00000000-0000-0000-0024-000000000004"), "escalations_export", 4, "تصدير التصعيدات", "Escalations Export" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ReportTitles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0024-000000000002"));

            migrationBuilder.DeleteData(
                table: "ReportTitles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0024-000000000003"));

            migrationBuilder.DeleteData(
                table: "ReportTitles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0024-000000000004"));
        }
    }
}
