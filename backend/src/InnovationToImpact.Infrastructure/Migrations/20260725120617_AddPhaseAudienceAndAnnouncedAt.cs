using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhaseAudienceAndAnnouncedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AnnouncedAt",
                table: "PhaseSchedules",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PhaseAudiences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhaseIdx = table.Column<int>(type: "int", nullable: false),
                    RoleCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhaseAudiences", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "PhaseAudiences",
                columns: new[] { "Id", "PhaseIdx", "RoleCode" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0035-000000000001"), 0, "submitter" },
                    { new Guid("00000000-0000-0000-0035-000000000002"), 1, "supervisor" },
                    { new Guid("00000000-0000-0000-0035-000000000003"), 2, "evaluator" },
                    { new Guid("00000000-0000-0000-0035-000000000004"), 3, "judge" },
                    { new Guid("00000000-0000-0000-0035-000000000005"), 3, "supervisor" },
                    { new Guid("00000000-0000-0000-0035-000000000006"), 4, "supervisor" },
                    { new Guid("00000000-0000-0000-0035-000000000007"), 5, "supervisor" },
                    { new Guid("00000000-0000-0000-0035-000000000008"), 6, "supervisor" }
                });

            migrationBuilder.UpdateData(
                table: "PhaseSchedules",
                keyColumn: "Idx",
                keyValue: 0,
                column: "AnnouncedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "PhaseSchedules",
                keyColumn: "Idx",
                keyValue: 1,
                column: "AnnouncedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "PhaseSchedules",
                keyColumn: "Idx",
                keyValue: 2,
                column: "AnnouncedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "PhaseSchedules",
                keyColumn: "Idx",
                keyValue: 3,
                column: "AnnouncedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "PhaseSchedules",
                keyColumn: "Idx",
                keyValue: 4,
                column: "AnnouncedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "PhaseSchedules",
                keyColumn: "Idx",
                keyValue: 5,
                column: "AnnouncedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "PhaseSchedules",
                keyColumn: "Idx",
                keyValue: 6,
                column: "AnnouncedAt",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_PhaseAudiences_PhaseIdx",
                table: "PhaseAudiences",
                column: "PhaseIdx");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhaseAudiences");

            migrationBuilder.DropColumn(
                name: "AnnouncedAt",
                table: "PhaseSchedules");
        }
    }
}
