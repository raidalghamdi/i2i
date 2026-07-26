using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupervisorMediationStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "IdeaStatuses",
                columns: new[] { "Id", "Code", "NameAr", "NameEn", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000023"), "evaluation_review", "مراجعة المشرف للتقييم", "Supervisor Evaluation Review", 23 },
                    { new Guid("00000000-0000-0000-0000-000000000024"), "submitter_review", "مراجعة مقدّم الفكرة", "Submitter Review", 24 },
                    { new Guid("00000000-0000-0000-0000-000000000025"), "committee_pending", "بانتظار الإحالة للجنة", "Awaiting Committee Referral", 25 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "IdeaStatuses",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000023"));

            migrationBuilder.DeleteData(
                table: "IdeaStatuses",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000024"));

            migrationBuilder.DeleteData(
                table: "IdeaStatuses",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000025"));
        }
    }
}
