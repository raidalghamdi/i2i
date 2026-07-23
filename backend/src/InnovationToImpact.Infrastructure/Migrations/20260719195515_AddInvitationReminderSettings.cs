using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitationReminderSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvitationReminderSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    CronExpression = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timezone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StopAfterNReminders = table.Column<int>(type: "int", nullable: false),
                    GapHours = table.Column<int>(type: "int", nullable: false),
                    ExpiresDays = table.Column<int>(type: "int", nullable: false),
                    FromName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FromEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProgramNameAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProgramNameEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvitationReminderSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "InvitationReminderSettings",
                columns: new[] { "Id", "CronExpression", "Enabled", "ExpiresDays", "FromEmail", "FromName", "GapHours", "ProgramNameAr", "ProgramNameEn", "StopAfterNReminders", "Timezone", "UpdatedAt", "UpdatedByUserId" },
                values: new object[] { new Guid("11111111-2222-3333-4444-555555555555"), "0 9 * * 1", true, 14, "noreply@gac.gov.sa", "Innovation-to-Impact Program", 48, "برنامج ابتكر لمنافس", "Innovation-to-Impact Program", 3, "Asia/Riyadh", new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Utc), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvitationReminderSettings");
        }
    }
}
