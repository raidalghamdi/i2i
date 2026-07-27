using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluationCriteria : Migration // Change 20260726
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EvaluationCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationCriteria", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "EvaluationCriteria",
                columns: new[] { "Id", "Active", "Code", "DescriptionAr", "DescriptionEn", "NameAr", "NameEn", "SortOrder", "Weight" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0031-000000000001"), true, "innovation", null, null, "الابتكار", "Innovation", 1, 0.20m },
                    { new Guid("00000000-0000-0000-0031-000000000002"), true, "impact", null, null, "الأثر", "Impact", 2, 0.20m },
                    { new Guid("00000000-0000-0000-0031-000000000003"), true, "execution", null, null, "قابلية التنفيذ", "Execution", 3, 0.20m },
                    { new Guid("00000000-0000-0000-0031-000000000004"), true, "scalability", null, null, "قابلية التوسع", "Scalability", 4, 0.20m },
                    { new Guid("00000000-0000-0000-0031-000000000005"), true, "presentation", null, null, "العرض والتقديم", "Presentation", 5, 0.20m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationCriteria_Code",
                table: "EvaluationCriteria",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvaluationCriteria");
        }
    }
}
