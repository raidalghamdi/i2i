using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedStrategicThemesAndSystemUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Department", "Email", "FullNameAr", "FullNameEn", "Level", "ManagerEmail", "Points", "SamAccountName", "Title" },
                values: new object[] { new Guid("00000000-0000-0000-0026-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "system@gac-demo.sa", "حساب النظام", "System Account", 1, null, 0, "system", null });

            migrationBuilder.InsertData(
                table: "StrategicThemes",
                columns: new[] { "Id", "DescriptionAr", "DescriptionEn", "NameAr", "NameEn", "OwnerId", "Priority" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0025-000000000001"), null, null, "التحول الرقمي", "Digital Transformation", new Guid("00000000-0000-0000-0026-000000000001"), 1 },
                    { new Guid("00000000-0000-0000-0025-000000000002"), null, null, "تجربة العملاء", "Customer Experience", new Guid("00000000-0000-0000-0026-000000000001"), 2 },
                    { new Guid("00000000-0000-0000-0025-000000000003"), null, null, "الكفاءة التشغيلية", "Operational Efficiency", new Guid("00000000-0000-0000-0026-000000000001"), 3 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "StrategicThemes",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0025-000000000001"));

            migrationBuilder.DeleteData(
                table: "StrategicThemes",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0025-000000000002"));

            migrationBuilder.DeleteData(
                table: "StrategicThemes",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0025-000000000003"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0026-000000000001"));
        }
    }
}
