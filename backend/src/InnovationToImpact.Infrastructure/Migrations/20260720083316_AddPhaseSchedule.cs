using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhaseSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhaseSchedules",
                columns: table => new
                {
                    Idx = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LabelAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LabelEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhaseSchedules", x => x.Idx);
                    table.ForeignKey(
                        name: "FK_PhaseSchedules_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "PhaseSchedules",
                columns: new[] { "Idx", "Code", "EndsAt", "LabelAr", "LabelEn", "StartsAt", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 0, "submission", null, "تقديم الأفكار", "Idea Submission", null, new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 1, "screening", null, "الفرز", "Screening", null, new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 2, "evaluation", null, "التقييم", "Evaluation", null, new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 3, "committee", null, "مراجعة اللجنة", "Committee Review", null, new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 4, "pilot", null, "التجريب", "Pilot", null, new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 5, "implementation", null, "التنفيذ", "Implementation", null, new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 6, "benefits_tracking", null, "تتبع الفوائد", "Benefits Tracking", null, new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhaseSchedules_UpdatedBy",
                table: "PhaseSchedules",
                column: "UpdatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhaseSchedules");
        }
    }
}
