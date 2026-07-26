using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdeaEvaluationAggregate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EvaluationAggregateJson",
                table: "Ideas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EvaluationAggregateScore",
                table: "Ideas",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EvaluationAggregateJson",
                table: "Ideas");

            migrationBuilder.DropColumn(
                name: "EvaluationAggregateScore",
                table: "Ideas");
        }
    }
}
