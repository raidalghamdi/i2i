using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluationCommitteeSlice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ValueJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminSettings", x => x.Key);
                    table.ForeignKey(
                        name: "FK_AdminSettings_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentStatuses",
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
                    table.PrimaryKey("PK_AssignmentStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommitteeCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DescriptionEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitteeCriteria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommitteeDecisionTypes",
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
                    table.PrimaryKey("PK_CommitteeDecisionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Evaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdeaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriteriaScoresJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalScore = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Recommendation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ConflictOfInterest = table.Column<bool>(type: "bit", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Evaluations_Ideas_IdeaId",
                        column: x => x.IdeaId,
                        principalTable: "Ideas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Evaluations_Users_EvaluatorId",
                        column: x => x.EvaluatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EvaluatorTrackAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrackId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluatorTrackAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluatorTrackAssignments_StrategicThemes_TrackId",
                        column: x => x.TrackId,
                        principalTable: "StrategicThemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluatorTrackAssignments_Users_AssignedById",
                        column: x => x.AssignedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluatorTrackAssignments_Users_EvaluatorId",
                        column: x => x.EvaluatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdeaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignmentStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assignments_AssignmentStatuses_AssignmentStatusId",
                        column: x => x.AssignmentStatusId,
                        principalTable: "AssignmentStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assignments_Ideas_IdeaId",
                        column: x => x.IdeaId,
                        principalTable: "Ideas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Assignments_Users_AssignedById",
                        column: x => x.AssignedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assignments_Users_EvaluatorId",
                        column: x => x.EvaluatorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommitteeDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdeaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommitteeName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CommitteeDecisionTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuorumMet = table.Column<bool>(type: "bit", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AttachmentsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecidedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitteeDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommitteeDecisions_CommitteeDecisionTypes_CommitteeDecisionTypeId",
                        column: x => x.CommitteeDecisionTypeId,
                        principalTable: "CommitteeDecisionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommitteeDecisions_Ideas_IdeaId",
                        column: x => x.IdeaId,
                        principalTable: "Ideas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommitteeDecisions_Users_DecidedById",
                        column: x => x.DecidedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "AdminSettings",
                columns: new[] { "Key", "UpdatedAt", "UpdatedById", "ValueJson" },
                values: new object[,]
                {
                    { "pass_threshold", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "7" },
                    { "top_n", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "5" }
                });

            migrationBuilder.InsertData(
                table: "AssignmentStatuses",
                columns: new[] { "Id", "Code", "NameAr", "NameEn", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0001-000000000001"), "pending", "معلّق", "Pending", 1 },
                    { new Guid("00000000-0000-0000-0001-000000000002"), "completed", "مكتمل", "Completed", 2 },
                    { new Guid("00000000-0000-0000-0001-000000000003"), "declined", "مرفوض", "Declined", 3 }
                });

            migrationBuilder.InsertData(
                table: "CommitteeCriteria",
                columns: new[] { "Id", "Active", "Code", "DescriptionAr", "DescriptionEn", "NameAr", "NameEn", "Weight" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0003-000000000001"), true, "originality", null, null, "الأصالة", "Originality", 0.30m },
                    { new Guid("00000000-0000-0000-0003-000000000002"), true, "feasibility", null, null, "قابلية التنفيذ", "Feasibility", 0.25m },
                    { new Guid("00000000-0000-0000-0003-000000000003"), true, "impact", null, null, "الأثر", "Impact", 0.30m },
                    { new Guid("00000000-0000-0000-0003-000000000004"), true, "alignment", null, null, "التوافق الاستراتيجي", "Strategic Alignment", 0.15m }
                });

            migrationBuilder.InsertData(
                table: "CommitteeDecisionTypes",
                columns: new[] { "Id", "Code", "NameAr", "NameEn", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0002-000000000001"), "approved", "معتمد", "Approved", 1 },
                    { new Guid("00000000-0000-0000-0002-000000000002"), "rejected", "مرفوض", "Rejected", 2 },
                    { new Guid("00000000-0000-0000-0002-000000000003"), "deferred", "مؤجل", "Deferred", 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminSettings_UpdatedById",
                table: "AdminSettings",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_AssignedById",
                table: "Assignments",
                column: "AssignedById");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_AssignmentStatusId",
                table: "Assignments",
                column: "AssignmentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_EvaluatorId_AssignmentStatusId",
                table: "Assignments",
                columns: new[] { "EvaluatorId", "AssignmentStatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_IdeaId",
                table: "Assignments",
                column: "IdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentStatuses_Code",
                table: "AssignmentStatuses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeCriteria_Code",
                table: "CommitteeCriteria",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeDecisions_CommitteeDecisionTypeId",
                table: "CommitteeDecisions",
                column: "CommitteeDecisionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeDecisions_DecidedById",
                table: "CommitteeDecisions",
                column: "DecidedById");

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeDecisions_IdeaId",
                table: "CommitteeDecisions",
                column: "IdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeDecisionTypes_Code",
                table: "CommitteeDecisionTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_EvaluatorId",
                table: "Evaluations",
                column: "EvaluatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_IdeaId_EvaluatorId",
                table: "Evaluations",
                columns: new[] { "IdeaId", "EvaluatorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluatorTrackAssignments_AssignedById",
                table: "EvaluatorTrackAssignments",
                column: "AssignedById");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluatorTrackAssignments_EvaluatorId_TrackId",
                table: "EvaluatorTrackAssignments",
                columns: new[] { "EvaluatorId", "TrackId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluatorTrackAssignments_TrackId",
                table: "EvaluatorTrackAssignments",
                column: "TrackId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminSettings");

            migrationBuilder.DropTable(
                name: "Assignments");

            migrationBuilder.DropTable(
                name: "CommitteeCriteria");

            migrationBuilder.DropTable(
                name: "CommitteeDecisions");

            migrationBuilder.DropTable(
                name: "Evaluations");

            migrationBuilder.DropTable(
                name: "EvaluatorTrackAssignments");

            migrationBuilder.DropTable(
                name: "AssignmentStatuses");

            migrationBuilder.DropTable(
                name: "CommitteeDecisionTypes");
        }
    }
}
