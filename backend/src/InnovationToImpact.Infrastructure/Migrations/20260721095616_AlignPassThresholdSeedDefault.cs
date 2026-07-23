using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignPassThresholdSeedDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AdminSettings",
                keyColumn: "Key",
                keyValue: "pass_threshold",
                column: "ValueJson",
                value: "6.0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AdminSettings",
                keyColumn: "Key",
                keyValue: "pass_threshold",
                column: "ValueJson",
                value: "7");
        }
    }
}
