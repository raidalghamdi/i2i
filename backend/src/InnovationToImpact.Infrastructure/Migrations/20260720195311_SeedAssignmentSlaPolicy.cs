using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedAssignmentSlaPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "SlaPolicies",
                columns: new[] { "Id", "EntityType", "FromState", "TargetHours", "ToState", "WarnAtPct" },
                values: new object[] { new Guid("00000000-0000-0000-0029-000000000001"), "assignment", "assignment", 72, "completed", 80 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SlaPolicies",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0029-000000000001"));
        }
    }
}
