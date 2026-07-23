using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPilotsBenefitsFundingSlice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BenefitCategories",
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
                    table.PrimaryKey("PK_BenefitCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BenefitTypes",
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
                    table.PrimaryKey("PK_BenefitTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FundingStatuses",
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
                    table.PrimaryKey("PK_FundingStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PilotStatuses",
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
                    table.PrimaryKey("PK_PilotStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Benefits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdeaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BenefitTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BenefitCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetValue = table.Column<decimal>(type: "decimal(16,2)", precision: 16, scale: 2, nullable: true),
                    RealizedValue = table.Column<decimal>(type: "decimal(16,2)", precision: 16, scale: 2, nullable: true),
                    MeasurementUnit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MeasurementDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EvidenceJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VerifiedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Benefits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Benefits_BenefitCategories_BenefitCategoryId",
                        column: x => x.BenefitCategoryId,
                        principalTable: "BenefitCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Benefits_BenefitTypes_BenefitTypeId",
                        column: x => x.BenefitTypeId,
                        principalTable: "BenefitTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Benefits_Ideas_IdeaId",
                        column: x => x.IdeaId,
                        principalTable: "Ideas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Benefits_Users_VerifiedById",
                        column: x => x.VerifiedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FundingRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdeaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmountSar = table.Column<decimal>(type: "decimal(16,2)", precision: 16, scale: 2, nullable: false),
                    JustificationAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JustificationEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FundingStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedAmount = table.Column<decimal>(type: "decimal(16,2)", precision: 16, scale: 2, nullable: true),
                    ApproverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundingRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundingRequests_FundingStatuses_FundingStatusId",
                        column: x => x.FundingStatusId,
                        principalTable: "FundingStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FundingRequests_Ideas_IdeaId",
                        column: x => x.IdeaId,
                        principalTable: "Ideas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FundingRequests_Users_ApproverId",
                        column: x => x.ApproverId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pilots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdeaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HypothesisAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HypothesisEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExperimentPlanAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExperimentPlanEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Budget = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MilestonesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultsAr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultsEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LessonsLearnedAr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LessonsLearnedEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PilotStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pilots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pilots_Ideas_IdeaId",
                        column: x => x.IdeaId,
                        principalTable: "Ideas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Pilots_PilotStatuses_PilotStatusId",
                        column: x => x.PilotStatusId,
                        principalTable: "PilotStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "BenefitCategories",
                columns: new[] { "Id", "Code", "NameAr", "NameEn", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0006-000000000001"), "financial", "مالي", "Financial", 1 },
                    { new Guid("00000000-0000-0000-0006-000000000002"), "operational", "تشغيلي", "Operational", 2 },
                    { new Guid("00000000-0000-0000-0006-000000000003"), "social", "اجتماعي", "Social", 3 },
                    { new Guid("00000000-0000-0000-0006-000000000004"), "strategic", "استراتيجي", "Strategic", 4 }
                });

            migrationBuilder.InsertData(
                table: "BenefitTypes",
                columns: new[] { "Id", "Code", "NameAr", "NameEn", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0005-000000000001"), "quantitative", "كمي", "Quantitative", 1 },
                    { new Guid("00000000-0000-0000-0005-000000000002"), "qualitative", "نوعي", "Qualitative", 2 }
                });

            migrationBuilder.InsertData(
                table: "FundingStatuses",
                columns: new[] { "Id", "Code", "NameAr", "NameEn", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0007-000000000001"), "pending", "قيد الانتظار", "Pending", 1 },
                    { new Guid("00000000-0000-0000-0007-000000000002"), "approved", "معتمد", "Approved", 2 },
                    { new Guid("00000000-0000-0000-0007-000000000003"), "rejected", "مرفوض", "Rejected", 3 },
                    { new Guid("00000000-0000-0000-0007-000000000004"), "partially_approved", "معتمد جزئياً", "Partially Approved", 4 }
                });

            migrationBuilder.InsertData(
                table: "PilotStatuses",
                columns: new[] { "Id", "Code", "NameAr", "NameEn", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0004-000000000001"), "planned", "مخطط له", "Planned", 1 },
                    { new Guid("00000000-0000-0000-0004-000000000002"), "in_progress", "قيد التنفيذ", "In Progress", 2 },
                    { new Guid("00000000-0000-0000-0004-000000000003"), "completed", "مكتمل", "Completed", 3 },
                    { new Guid("00000000-0000-0000-0004-000000000004"), "cancelled", "ملغى", "Cancelled", 4 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BenefitCategories_Code",
                table: "BenefitCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Benefits_BenefitCategoryId",
                table: "Benefits",
                column: "BenefitCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Benefits_BenefitTypeId",
                table: "Benefits",
                column: "BenefitTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Benefits_IdeaId",
                table: "Benefits",
                column: "IdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_Benefits_VerifiedById",
                table: "Benefits",
                column: "VerifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_BenefitTypes_Code",
                table: "BenefitTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FundingRequests_ApproverId",
                table: "FundingRequests",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_FundingRequests_FundingStatusId",
                table: "FundingRequests",
                column: "FundingStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_FundingRequests_IdeaId",
                table: "FundingRequests",
                column: "IdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_FundingStatuses_Code",
                table: "FundingStatuses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pilots_IdeaId",
                table: "Pilots",
                column: "IdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_Pilots_PilotStatusId",
                table: "Pilots",
                column: "PilotStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_PilotStatuses_Code",
                table: "PilotStatuses",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Benefits");

            migrationBuilder.DropTable(
                name: "FundingRequests");

            migrationBuilder.DropTable(
                name: "Pilots");

            migrationBuilder.DropTable(
                name: "BenefitCategories");

            migrationBuilder.DropTable(
                name: "BenefitTypes");

            migrationBuilder.DropTable(
                name: "FundingStatuses");

            migrationBuilder.DropTable(
                name: "PilotStatuses");
        }
    }
}
