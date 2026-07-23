using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalStepMinApprovers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LabelAr",
                table: "ApprovalChainSteps",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LabelEn",
                table: "ApprovalChainSteps",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MinApprovers",
                table: "ApprovalChainSteps",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.InsertData(
                table: "ApprovalChains",
                columns: new[] { "Id", "Code", "EntityType", "IsActive", "NameAr", "NameEn" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0032-000000000001"), "committee-publish", "committee_decision", true, "نشر قرار اللجنة", "Committee publish" },
                    { new Guid("00000000-0000-0000-0032-000000000002"), "idea-approve", "idea", true, "اعتماد الفكرة", "Idea approval" }
                });

            migrationBuilder.InsertData(
                table: "ApprovalChainSteps",
                columns: new[] { "Id", "ApprovalChainId", "IsRequired", "LabelAr", "LabelEn", "MinApprovers", "RoleId", "StepOrder" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0031-000000000001"), new Guid("00000000-0000-0000-0032-000000000001"), true, "تقييم المُقيّم", "Evaluator scoring", 1, new Guid("00000000-0000-0000-0023-000000000004"), 1 },
                    { new Guid("00000000-0000-0000-0031-000000000002"), new Guid("00000000-0000-0000-0032-000000000001"), true, "موافقة اثنين من ثلاثة محكّمين", "2-of-3 judges approve", 2, new Guid("00000000-0000-0000-0023-000000000003"), 2 },
                    { new Guid("00000000-0000-0000-0031-000000000003"), new Guid("00000000-0000-0000-0032-000000000002"), true, "موافقة المشرف", "Admin approval", 1, new Guid("00000000-0000-0000-0023-000000000001"), 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ApprovalChainSteps",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0031-000000000001"));

            migrationBuilder.DeleteData(
                table: "ApprovalChainSteps",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0031-000000000002"));

            migrationBuilder.DeleteData(
                table: "ApprovalChainSteps",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0031-000000000003"));

            migrationBuilder.DeleteData(
                table: "ApprovalChains",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0032-000000000001"));

            migrationBuilder.DeleteData(
                table: "ApprovalChains",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0032-000000000002"));

            migrationBuilder.DropColumn(
                name: "LabelAr",
                table: "ApprovalChainSteps");

            migrationBuilder.DropColumn(
                name: "LabelEn",
                table: "ApprovalChainSteps");

            migrationBuilder.DropColumn(
                name: "MinApprovers",
                table: "ApprovalChainSteps");
        }
    }
}
