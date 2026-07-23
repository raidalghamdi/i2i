using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScreeningAndRankingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Ideas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FinalRank",
                table: "Ideas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScreeningReason",
                table: "Ideas",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Ideas");

            migrationBuilder.DropColumn(
                name: "FinalRank",
                table: "Ideas");

            migrationBuilder.DropColumn(
                name: "ScreeningReason",
                table: "Ideas");
        }
    }
}
