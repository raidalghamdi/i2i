using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChallengesTeamMembersAndIdeaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ChallengeId",
                table: "Ideas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EditableSections",
                table: "Ideas",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IpAcknowledged",
                table: "Ideas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ParticipationType",
                table: "Ideas",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "individual");

            migrationBuilder.AddColumn<string>(
                name: "TeamName",
                table: "Ideas",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TermsAgreed",
                table: "Ideas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Challenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StrategicThemeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TextAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TextEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Challenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Challenges_StrategicThemes_StrategicThemeId",
                        column: x => x.StrategicThemeId,
                        principalTable: "StrategicThemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdeaTeamMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdeaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdeaTeamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdeaTeamMembers_Ideas_IdeaId",
                        column: x => x.IdeaId,
                        principalTable: "Ideas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ideas_ChallengeId",
                table: "Ideas",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_StrategicThemeId",
                table: "Challenges",
                column: "StrategicThemeId");

            migrationBuilder.CreateIndex(
                name: "IX_IdeaTeamMembers_IdeaId",
                table: "IdeaTeamMembers",
                column: "IdeaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ideas_Challenges_ChallengeId",
                table: "Ideas",
                column: "ChallengeId",
                principalTable: "Challenges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ideas_Challenges_ChallengeId",
                table: "Ideas");

            migrationBuilder.DropTable(
                name: "Challenges");

            migrationBuilder.DropTable(
                name: "IdeaTeamMembers");

            migrationBuilder.DropIndex(
                name: "IX_Ideas_ChallengeId",
                table: "Ideas");

            migrationBuilder.DropColumn(
                name: "ChallengeId",
                table: "Ideas");

            migrationBuilder.DropColumn(
                name: "EditableSections",
                table: "Ideas");

            migrationBuilder.DropColumn(
                name: "IpAcknowledged",
                table: "Ideas");

            migrationBuilder.DropColumn(
                name: "ParticipationType",
                table: "Ideas");

            migrationBuilder.DropColumn(
                name: "TeamName",
                table: "Ideas");

            migrationBuilder.DropColumn(
                name: "TermsAgreed",
                table: "Ideas");
        }
    }
}
