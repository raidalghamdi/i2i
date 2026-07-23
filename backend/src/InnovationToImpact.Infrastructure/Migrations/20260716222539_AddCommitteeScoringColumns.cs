using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommitteeScoringColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CommitteeDecisions_IdeaId",
                table: "CommitteeDecisions");

            migrationBuilder.AddColumn<decimal>(
                name: "CommitteeFinalScore",
                table: "Ideas",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CriteriaScoresJson",
                table: "CommitteeDecisions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalScore",
                table: "CommitteeDecisions",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeDecisions_IdeaId_DecidedById",
                table: "CommitteeDecisions",
                columns: new[] { "IdeaId", "DecidedById" },
                unique: true,
                filter: "[DecidedById] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CommitteeDecisions_IdeaId_DecidedById",
                table: "CommitteeDecisions");

            migrationBuilder.DropColumn(
                name: "CommitteeFinalScore",
                table: "Ideas");

            migrationBuilder.DropColumn(
                name: "CriteriaScoresJson",
                table: "CommitteeDecisions");

            migrationBuilder.DropColumn(
                name: "TotalScore",
                table: "CommitteeDecisions");

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeDecisions_IdeaId",
                table: "CommitteeDecisions",
                column: "IdeaId");
        }
    }
}
