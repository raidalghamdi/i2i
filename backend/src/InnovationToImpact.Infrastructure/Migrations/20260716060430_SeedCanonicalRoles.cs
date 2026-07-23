using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedCanonicalRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Code", "DescriptionAr", "DescriptionEn", "IsActive", "IsSystem", "NameAr", "NameEn", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0023-000000000001"), "admin", null, null, true, true, "مسؤول", "Admin", 1 },
                    { new Guid("00000000-0000-0000-0023-000000000002"), "supervisor", null, null, true, true, "مشرف", "Supervisor", 2 },
                    { new Guid("00000000-0000-0000-0023-000000000003"), "judge", null, null, true, true, "لجنة التحكيم", "Judge", 3 },
                    { new Guid("00000000-0000-0000-0023-000000000004"), "evaluator", null, null, true, true, "مقيّم", "Evaluator", 4 },
                    { new Guid("00000000-0000-0000-0023-000000000005"), "submitter", null, null, true, true, "مقدم الفكرة", "Submitter", 5 },
                    { new Guid("00000000-0000-0000-0023-000000000006"), "expert", null, null, true, true, "خبير", "Expert", 6 },
                    { new Guid("00000000-0000-0000-0023-000000000007"), "mentor", null, null, true, true, "موجه", "Mentor", 7 },
                    { new Guid("00000000-0000-0000-0023-000000000008"), "facilitator", null, null, true, true, "ميسّر", "Facilitator", 8 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0023-000000000001"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0023-000000000002"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0023-000000000003"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0023-000000000004"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0023-000000000005"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0023-000000000006"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0023-000000000007"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0023-000000000008"));
        }
    }
}
