using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImplementationScaleDecisionSlice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HandoverStatuses",
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
                    table.PrimaryKey("PK_HandoverStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScaleDecisionTypes",
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
                    table.PrimaryKey("PK_ScaleDecisionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Implementations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdeaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationalOwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationPlanAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IntegrationPlanEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResourceCommitmentAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResourceCommitmentEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HandoverStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineUnit = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Implementations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Implementations_HandoverStatuses_HandoverStatusId",
                        column: x => x.HandoverStatusId,
                        principalTable: "HandoverStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Implementations_Ideas_IdeaId",
                        column: x => x.IdeaId,
                        principalTable: "Ideas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Implementations_Users_OperationalOwnerId",
                        column: x => x.OperationalOwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScaleDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdeaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceOfViabilityAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvidenceOfViabilityEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValueAssessmentAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValueAssessmentEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RiskAssessmentAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RiskAssessmentEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StrategicFitScore = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false),
                    ScaleDecisionTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecidedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScaleDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScaleDecisions_Ideas_IdeaId",
                        column: x => x.IdeaId,
                        principalTable: "Ideas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScaleDecisions_ScaleDecisionTypes_ScaleDecisionTypeId",
                        column: x => x.ScaleDecisionTypeId,
                        principalTable: "ScaleDecisionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScaleDecisions_Users_DecidedById",
                        column: x => x.DecidedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "HandoverStatuses",
                columns: new[] { "Id", "Code", "NameAr", "NameEn", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0009-000000000001"), "pending", "قيد الانتظار", "Pending", 1 },
                    { new Guid("00000000-0000-0000-0009-000000000002"), "in_progress", "قيد التنفيذ", "In Progress", 2 },
                    { new Guid("00000000-0000-0000-0009-000000000003"), "completed", "مكتمل", "Completed", 3 }
                });

            migrationBuilder.InsertData(
                table: "ScaleDecisionTypes",
                columns: new[] { "Id", "Code", "NameAr", "NameEn", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0008-000000000001"), "scale", "توسيع", "Scale", 1 },
                    { new Guid("00000000-0000-0000-0008-000000000002"), "hold", "تعليق", "Hold", 2 },
                    { new Guid("00000000-0000-0000-0008-000000000003"), "reject", "رفض", "Reject", 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_HandoverStatuses_Code",
                table: "HandoverStatuses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Implementations_HandoverStatusId",
                table: "Implementations",
                column: "HandoverStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Implementations_IdeaId",
                table: "Implementations",
                column: "IdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_Implementations_OperationalOwnerId",
                table: "Implementations",
                column: "OperationalOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ScaleDecisions_DecidedById",
                table: "ScaleDecisions",
                column: "DecidedById");

            migrationBuilder.CreateIndex(
                name: "IX_ScaleDecisions_IdeaId",
                table: "ScaleDecisions",
                column: "IdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_ScaleDecisions_ScaleDecisionTypeId",
                table: "ScaleDecisions",
                column: "ScaleDecisionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ScaleDecisionTypes_Code",
                table: "ScaleDecisionTypes",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Implementations");

            migrationBuilder.DropTable(
                name: "ScaleDecisions");

            migrationBuilder.DropTable(
                name: "HandoverStatuses");

            migrationBuilder.DropTable(
                name: "ScaleDecisionTypes");
        }
    }
}
