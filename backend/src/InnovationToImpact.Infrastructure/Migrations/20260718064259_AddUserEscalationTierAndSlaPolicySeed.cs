using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserEscalationTierAndSlaPolicySeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EscalationTierId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.InsertData(
                table: "SlaPolicies",
                columns: new[] { "Id", "EntityType", "FromState", "TargetHours", "ToState", "WarnAtPct" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0027-000000000001"), "evaluation", "evaluation", 72, "evaluated", 80 },
                    { new Guid("00000000-0000-0000-0027-000000000002"), "committee", "committee", 168, "decided", 80 }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0026-000000000001"),
                column: "EscalationTierId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Users_EscalationTierId",
                table: "Users",
                column: "EscalationTierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_EscalationTiers_EscalationTierId",
                table: "Users",
                column: "EscalationTierId",
                principalTable: "EscalationTiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_EscalationTiers_EscalationTierId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_EscalationTierId",
                table: "Users");

            migrationBuilder.DeleteData(
                table: "SlaPolicies",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0027-000000000001"));

            migrationBuilder.DeleteData(
                table: "SlaPolicies",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0027-000000000002"));

            migrationBuilder.DropColumn(
                name: "EscalationTierId",
                table: "Users");
        }
    }
}
