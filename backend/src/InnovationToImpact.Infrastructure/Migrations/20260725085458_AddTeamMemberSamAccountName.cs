using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamMemberSamAccountName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SamAccountName",
                table: "IdeaTeamMembers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SamAccountName",
                table: "IdeaTeamMembers");
        }
    }
}
