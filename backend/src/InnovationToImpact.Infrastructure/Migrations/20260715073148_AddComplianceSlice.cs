using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddComplianceSlice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComplianceControlStatuses",
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
                    table.PrimaryKey("PK_ComplianceControlStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StandardBodies",
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
                    table.PrimaryKey("PK_StandardBodies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceControls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ControlCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StandardBodyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DescriptionEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MappedFeaturePathsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvidenceUrlsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ComplianceControlStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceControls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplianceControls_ComplianceControlStatuses_ComplianceControlStatusId",
                        column: x => x.ComplianceControlStatusId,
                        principalTable: "ComplianceControlStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComplianceControls_StandardBodies_StandardBodyId",
                        column: x => x.StandardBodyId,
                        principalTable: "StandardBodies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComplianceControls_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ComplianceControlStatuses",
                columns: new[] { "Id", "Code", "NameAr", "NameEn", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0014-000000000001"), "not_started", "لم يبدأ", "Not Started", 1 },
                    { new Guid("00000000-0000-0000-0014-000000000002"), "in_progress", "قيد التنفيذ", "In Progress", 2 },
                    { new Guid("00000000-0000-0000-0014-000000000003"), "met", "محقق", "Met", 3 },
                    { new Guid("00000000-0000-0000-0014-000000000004"), "not_applicable", "غير قابل للتطبيق", "Not Applicable", 4 }
                });

            migrationBuilder.InsertData(
                table: "StandardBodies",
                columns: new[] { "Id", "Code", "NameAr", "NameEn", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0013-000000000001"), "sdaia_ndmo", "سدايا/المكتب الوطني لإدارة البيانات", "SDAIA/NDMO", 1 },
                    { new Guid("00000000-0000-0000-0013-000000000002"), "nca", "الهيئة الوطنية للأمن السيبراني", "NCA", 2 },
                    { new Guid("00000000-0000-0000-0013-000000000003"), "dga", "هيئة الحكومة الرقمية", "DGA", 3 },
                    { new Guid("00000000-0000-0000-0013-000000000004"), "cst", "هيئة الاتصالات والفضاء والتقنية", "CST", 4 },
                    { new Guid("00000000-0000-0000-0013-000000000005"), "rdia", "هيئة البحث والتطوير والابتكار", "RDIA", 5 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceControls_ComplianceControlStatusId",
                table: "ComplianceControls",
                column: "ComplianceControlStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceControls_ControlCode",
                table: "ComplianceControls",
                column: "ControlCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceControls_OwnerId",
                table: "ComplianceControls",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceControls_StandardBodyId",
                table: "ComplianceControls",
                column: "StandardBodyId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceControlStatuses_Code",
                table: "ComplianceControlStatuses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StandardBodies_Code",
                table: "StandardBodies",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComplianceControls");

            migrationBuilder.DropTable(
                name: "ComplianceControlStatuses");

            migrationBuilder.DropTable(
                name: "StandardBodies");
        }
    }
}
