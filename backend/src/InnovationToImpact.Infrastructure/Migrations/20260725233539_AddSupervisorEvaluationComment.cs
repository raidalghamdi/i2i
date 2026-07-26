using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupervisorEvaluationComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SupervisorEvaluationComment",
                table: "Ideas",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupervisorEvaluationComment",
                table: "Ideas");
        }
    }
}
