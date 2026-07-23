using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedRoleInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoleInvitationSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    DefaultExpiresDays = table.Column<int>(type: "int", nullable: false),
                    ReminderGapHours = table.Column<int>(type: "int", nullable: false),
                    MaxReminders = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleInvitationSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleInvitationStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleInvitationStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SamAccountName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    RoleInvitationStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeadlineAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReminderCount = table.Column<int>(type: "int", nullable: false),
                    LastReminderAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InvitedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleInvitations_RoleInvitationStatuses_RoleInvitationStatusId",
                        column: x => x.RoleInvitationStatusId,
                        principalTable: "RoleInvitationStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoleInvitations_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoleInvitations_Users_InvitedById",
                        column: x => x.InvitedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "RoleInvitationSettings",
                columns: new[] { "Id", "DefaultExpiresDays", "Enabled", "MaxReminders", "ReminderGapHours", "UpdatedAt", "UpdatedByUserId" },
                values: new object[] { new Guid("22222222-3333-4444-5555-666666666666"), 14, true, 3, 48, new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.InsertData(
                table: "RoleInvitationStatuses",
                columns: new[] { "Id", "Code", "NameAr", "NameEn", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0030-000000000001"), "pending", "قيد الانتظار", "Pending", 1 },
                    { new Guid("00000000-0000-0000-0030-000000000002"), "applied", "مطبّقة", "Applied", 2 },
                    { new Guid("00000000-0000-0000-0030-000000000003"), "expired", "منتهية الصلاحية", "Expired", 3 },
                    { new Guid("00000000-0000-0000-0030-000000000004"), "withdrawn", "مسحوبة", "Withdrawn", 4 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleInvitations_InvitedById",
                table: "RoleInvitations",
                column: "InvitedById");

            migrationBuilder.CreateIndex(
                name: "IX_RoleInvitations_RoleId",
                table: "RoleInvitations",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleInvitations_RoleInvitationStatusId",
                table: "RoleInvitations",
                column: "RoleInvitationStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleInvitations_SamAccountName_RoleId_RoleInvitationStatusId",
                table: "RoleInvitations",
                columns: new[] { "SamAccountName", "RoleId", "RoleInvitationStatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_RoleInvitationStatuses_Code",
                table: "RoleInvitationStatuses",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleInvitations");

            migrationBuilder.DropTable(
                name: "RoleInvitationSettings");

            migrationBuilder.DropTable(
                name: "RoleInvitationStatuses");
        }
    }
}
