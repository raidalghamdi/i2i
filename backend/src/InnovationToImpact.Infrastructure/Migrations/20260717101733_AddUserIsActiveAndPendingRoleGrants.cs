using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIsActiveAndPendingRoleGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "PendingRoleGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SamAccountName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrantedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingRoleGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingRoleGrants_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PendingRoleGrants_Users_GrantedById",
                        column: x => x.GrantedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0026-000000000001"),
                column: "IsActive",
                value: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingRoleGrants_GrantedById",
                table: "PendingRoleGrants",
                column: "GrantedById");

            migrationBuilder.CreateIndex(
                name: "IX_PendingRoleGrants_RoleId",
                table: "PendingRoleGrants",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingRoleGrants_SamAccountName_RoleId",
                table: "PendingRoleGrants",
                columns: new[] { "SamAccountName", "RoleId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingRoleGrants");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");
        }
    }
}
