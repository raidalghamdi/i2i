using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalsSlice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApprovalChains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalChains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalDecisionTypes",
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
                    table.PrimaryKey("PK_ApprovalDecisionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalInstanceStatuses",
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
                    table.PrimaryKey("PK_ApprovalInstanceStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalChainSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalChainId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalChainSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalChainSteps_ApprovalChains_ApprovalChainId",
                        column: x => x.ApprovalChainId,
                        principalTable: "ApprovalChains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalChainSteps_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalChainId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalInstanceStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentStepOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalInstances_ApprovalChains_ApprovalChainId",
                        column: x => x.ApprovalChainId,
                        principalTable: "ApprovalChains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalInstances_ApprovalInstanceStatuses_ApprovalInstanceStatusId",
                        column: x => x.ApprovalInstanceStatusId,
                        principalTable: "ApprovalInstanceStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalStepDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalChainStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeciderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalDecisionTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommentsAr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommentsEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalStepDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalStepDecisions_ApprovalChainSteps_ApprovalChainStepId",
                        column: x => x.ApprovalChainStepId,
                        principalTable: "ApprovalChainSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalStepDecisions_ApprovalDecisionTypes_ApprovalDecisionTypeId",
                        column: x => x.ApprovalDecisionTypeId,
                        principalTable: "ApprovalDecisionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalStepDecisions_ApprovalInstances_ApprovalInstanceId",
                        column: x => x.ApprovalInstanceId,
                        principalTable: "ApprovalInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalStepDecisions_Users_DeciderId",
                        column: x => x.DeciderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ApprovalDecisionTypes",
                columns: new[] { "Id", "Code", "NameAr", "NameEn", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0022-000000000001"), "approve", "موافقة", "Approve", 1 },
                    { new Guid("00000000-0000-0000-0022-000000000002"), "reject", "رفض", "Reject", 2 },
                    { new Guid("00000000-0000-0000-0022-000000000003"), "request_changes", "طلب تعديلات", "Request Changes", 3 }
                });

            migrationBuilder.InsertData(
                table: "ApprovalInstanceStatuses",
                columns: new[] { "Id", "Code", "NameAr", "NameEn", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0021-000000000001"), "pending", "قيد الانتظار", "Pending", 1 },
                    { new Guid("00000000-0000-0000-0021-000000000002"), "approved", "معتمد", "Approved", 2 },
                    { new Guid("00000000-0000-0000-0021-000000000003"), "rejected", "مرفوض", "Rejected", 3 },
                    { new Guid("00000000-0000-0000-0021-000000000004"), "cancelled", "ملغى", "Cancelled", 4 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalChains_Code",
                table: "ApprovalChains",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalChainSteps_ApprovalChainId_StepOrder",
                table: "ApprovalChainSteps",
                columns: new[] { "ApprovalChainId", "StepOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalChainSteps_RoleId",
                table: "ApprovalChainSteps",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecisionTypes_Code",
                table: "ApprovalDecisionTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalInstances_ApprovalChainId",
                table: "ApprovalInstances",
                column: "ApprovalChainId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalInstances_ApprovalInstanceStatusId",
                table: "ApprovalInstances",
                column: "ApprovalInstanceStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalInstanceStatuses_Code",
                table: "ApprovalInstanceStatuses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalStepDecisions_ApprovalChainStepId",
                table: "ApprovalStepDecisions",
                column: "ApprovalChainStepId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalStepDecisions_ApprovalDecisionTypeId",
                table: "ApprovalStepDecisions",
                column: "ApprovalDecisionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalStepDecisions_ApprovalInstanceId",
                table: "ApprovalStepDecisions",
                column: "ApprovalInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalStepDecisions_DeciderId",
                table: "ApprovalStepDecisions",
                column: "DeciderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalStepDecisions");

            migrationBuilder.DropTable(
                name: "ApprovalChainSteps");

            migrationBuilder.DropTable(
                name: "ApprovalDecisionTypes");

            migrationBuilder.DropTable(
                name: "ApprovalInstances");

            migrationBuilder.DropTable(
                name: "ApprovalChains");

            migrationBuilder.DropTable(
                name: "ApprovalInstanceStatuses");
        }
    }
}
