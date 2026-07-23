using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSlaEscalationsSlice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EscalationStatuses",
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
                    table.PrimaryKey("PK_EscalationStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EscalationTiers",
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
                    table.PrimaryKey("PK_EscalationTiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SlaPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FromState = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ToState = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TargetHours = table.Column<int>(type: "int", nullable: false),
                    WarnAtPct = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlaPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Escalations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EscalationTierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReasonAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReasonEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResolutionAr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResolutionEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EscalationStatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Escalations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Escalations_EscalationStatuses_EscalationStatusId",
                        column: x => x.EscalationStatusId,
                        principalTable: "EscalationStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Escalations_EscalationTiers_EscalationTierId",
                        column: x => x.EscalationTierId,
                        principalTable: "EscalationTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Escalations_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SlaTrackings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SlaPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TargetAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BreachedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlaTrackings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlaTrackings_SlaPolicies_SlaPolicyId",
                        column: x => x.SlaPolicyId,
                        principalTable: "SlaPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EscalationEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EscalationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FromTierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToTierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscalationEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EscalationEvents_EscalationTiers_FromTierId",
                        column: x => x.FromTierId,
                        principalTable: "EscalationTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EscalationEvents_EscalationTiers_ToTierId",
                        column: x => x.ToTierId,
                        principalTable: "EscalationTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EscalationEvents_Escalations_EscalationId",
                        column: x => x.EscalationId,
                        principalTable: "Escalations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EscalationEvents_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "EscalationStatuses",
                columns: new[] { "Id", "Code", "NameAr", "NameEn", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0020-000000000001"), "open", "مفتوح", "Open", 1 },
                    { new Guid("00000000-0000-0000-0020-000000000002"), "acknowledged", "تم الإقرار", "Acknowledged", 2 },
                    { new Guid("00000000-0000-0000-0020-000000000003"), "resolved", "تم الحل", "Resolved", 3 },
                    { new Guid("00000000-0000-0000-0020-000000000004"), "cancelled", "ملغى", "Cancelled", 4 }
                });

            migrationBuilder.InsertData(
                table: "EscalationTiers",
                columns: new[] { "Id", "Code", "NameAr", "NameEn", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0019-000000000001"), "manager", "المدير المباشر", "Manager", 1 },
                    { new Guid("00000000-0000-0000-0019-000000000002"), "director", "المدير العام", "Director", 2 },
                    { new Guid("00000000-0000-0000-0019-000000000003"), "exec", "الإدارة التنفيذية", "Executive", 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_EscalationEvents_ActorId",
                table: "EscalationEvents",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalationEvents_EscalationId",
                table: "EscalationEvents",
                column: "EscalationId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalationEvents_FromTierId",
                table: "EscalationEvents",
                column: "FromTierId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalationEvents_ToTierId",
                table: "EscalationEvents",
                column: "ToTierId");

            migrationBuilder.CreateIndex(
                name: "IX_Escalations_EscalationStatusId",
                table: "Escalations",
                column: "EscalationStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Escalations_EscalationTierId",
                table: "Escalations",
                column: "EscalationTierId");

            migrationBuilder.CreateIndex(
                name: "IX_Escalations_OwnerId",
                table: "Escalations",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalationStatuses_Code",
                table: "EscalationStatuses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EscalationTiers_Code",
                table: "EscalationTiers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlaPolicies_EntityType_FromState_ToState",
                table: "SlaPolicies",
                columns: new[] { "EntityType", "FromState", "ToState" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlaTrackings_SlaPolicyId",
                table: "SlaTrackings",
                column: "SlaPolicyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EscalationEvents");

            migrationBuilder.DropTable(
                name: "SlaTrackings");

            migrationBuilder.DropTable(
                name: "Escalations");

            migrationBuilder.DropTable(
                name: "SlaPolicies");

            migrationBuilder.DropTable(
                name: "EscalationStatuses");

            migrationBuilder.DropTable(
                name: "EscalationTiers");
        }
    }
}
