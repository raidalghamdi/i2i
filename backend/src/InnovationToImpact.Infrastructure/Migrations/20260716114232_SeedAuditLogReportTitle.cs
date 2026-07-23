using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedAuditLogReportTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ReportTitles",
                columns: new[] { "Id", "Key", "SortOrder", "TitleAr", "TitleEn" },
                values: new object[] { new Guid("00000000-0000-0000-0024-000000000001"), "audit_log_export", 1, "تصدير سجل التدقيق", "Audit Log Export" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ReportTitles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0024-000000000001"));
        }
    }
}
