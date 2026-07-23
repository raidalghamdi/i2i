using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IdeaStatuses",
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
                    table.PrimaryKey("PK_IdeaStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SamAccountName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    FullNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FullNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ManagerEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Points = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activities_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StrategicThemes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategicThemes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrategicThemes_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ideas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ProblemStatementAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProblemStatementEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProposedSolutionAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProposedSolutionEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpectedBenefitsAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpectedBenefitsEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StrategicThemeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdeaStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentStage = table.Column<int>(type: "int", nullable: false),
                    SubmitterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ideas", x => x.Id);
                    table.CheckConstraint("CK_Ideas_CurrentStage", "[CurrentStage] >= 0 AND [CurrentStage] <= 8");
                    table.ForeignKey(
                        name: "FK_Ideas_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ideas_IdeaStatuses_IdeaStatusId",
                        column: x => x.IdeaStatusId,
                        principalTable: "IdeaStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ideas_StrategicThemes_StrategicThemeId",
                        column: x => x.StrategicThemeId,
                        principalTable: "StrategicThemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ideas_Users_SubmitterId",
                        column: x => x.SubmitterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "IdeaStatuses",
                columns: new[] { "Id", "Code", "NameAr", "NameEn", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), "draft", "مسودة", "Draft", 1 },
                    { new Guid("00000000-0000-0000-0000-000000000002"), "submitted", "مُقدَّمة", "Submitted", 2 },
                    { new Guid("00000000-0000-0000-0000-000000000003"), "screening", "قيد الفرز", "Screening", 3 },
                    { new Guid("00000000-0000-0000-0000-000000000004"), "needs_completion", "بحاجة لإكمال", "Needs Completion", 4 },
                    { new Guid("00000000-0000-0000-0000-000000000005"), "evaluation", "قيد التقييم", "Evaluation", 5 },
                    { new Guid("00000000-0000-0000-0000-000000000006"), "committee", "قيد اللجنة", "Committee", 6 },
                    { new Guid("00000000-0000-0000-0000-000000000007"), "approved", "معتمدة", "Approved", 7 },
                    { new Guid("00000000-0000-0000-0000-000000000008"), "rejected", "مرفوضة", "Rejected", 8 },
                    { new Guid("00000000-0000-0000-0000-000000000009"), "returned", "معادة", "Returned", 9 },
                    { new Guid("00000000-0000-0000-0000-000000000010"), "assigned", "مسندة", "Assigned", 10 },
                    { new Guid("00000000-0000-0000-0000-000000000011"), "in_pilot", "قيد التجريب", "In Pilot", 11 },
                    { new Guid("00000000-0000-0000-0000-000000000012"), "in_implementation", "قيد التنفيذ", "In Implementation", 12 },
                    { new Guid("00000000-0000-0000-0000-000000000013"), "benefits_tracking", "تتبع المنافع", "Benefits Tracking", 13 },
                    { new Guid("00000000-0000-0000-0000-000000000014"), "closed", "مغلقة", "Closed", 14 },
                    { new Guid("00000000-0000-0000-0000-000000000015"), "archived", "مؤرشفة", "Archived", 15 },
                    { new Guid("00000000-0000-0000-0000-000000000016"), "withdrawn", "مسحوبة", "Withdrawn", 16 },
                    { new Guid("00000000-0000-0000-0000-000000000017"), "pass_awaiting_attachments", "بانتظار المرفقات", "Pass Awaiting Attachments", 17 },
                    { new Guid("00000000-0000-0000-0000-000000000018"), "pending_final_ranking", "بانتظار الترتيب النهائي", "Pending Final Ranking", 18 },
                    { new Guid("00000000-0000-0000-0000-000000000019"), "evaluation_failed", "فشل التقييم", "Evaluation Failed", 19 },
                    { new Guid("00000000-0000-0000-0000-000000000020"), "not_selected", "لم يتم اختيارها", "Not Selected", 20 },
                    { new Guid("00000000-0000-0000-0000-000000000021"), "in_measurement", "قيد القياس", "In Measurement", 21 },
                    { new Guid("00000000-0000-0000-0000-000000000022"), "in_scaling", "قيد التوسّع", "In Scaling", 22 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_CreatedById",
                table: "Activities",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Ideas_ActivityId",
                table: "Ideas",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_Ideas_Code",
                table: "Ideas",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ideas_IdeaStatusId",
                table: "Ideas",
                column: "IdeaStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Ideas_StrategicThemeId",
                table: "Ideas",
                column: "StrategicThemeId");

            migrationBuilder.CreateIndex(
                name: "IX_Ideas_SubmitterId",
                table: "Ideas",
                column: "SubmitterId");

            migrationBuilder.CreateIndex(
                name: "IX_IdeaStatuses_Code",
                table: "IdeaStatuses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Code",
                table: "Roles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StrategicThemes_OwnerId",
                table: "StrategicThemes",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_SamAccountName",
                table: "Users",
                column: "SamAccountName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ideas");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "IdeaStatuses");

            migrationBuilder.DropTable(
                name: "StrategicThemes");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
