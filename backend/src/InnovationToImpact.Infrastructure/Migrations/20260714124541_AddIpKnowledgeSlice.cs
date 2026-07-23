using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIpKnowledgeSlice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IpSignatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdeaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IpTermsVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpSignatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IpSignatures_Ideas_IdeaId",
                        column: x => x.IdeaId,
                        principalTable: "Ideas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IpSignatures_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IpTypes",
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
                    table.PrimaryKey("PK_IpTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeTypes",
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
                    table.PrimaryKey("PK_KnowledgeTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeVisibilities",
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
                    table.PrimaryKey("PK_KnowledgeVisibilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IpRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdeaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IpTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnershipPartyAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnershipPartyEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConfidentialityTermsAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConfidentialityTermsEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParticipationConditionsAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParticipationConditionsEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NdaRequired = table.Column<bool>(type: "bit", nullable: false),
                    NdaSignedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IpRecords_Ideas_IdeaId",
                        column: x => x.IdeaId,
                        principalTable: "Ideas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IpRecords_IpTypes_IpTypeId",
                        column: x => x.IpTypeId,
                        principalTable: "IpTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeArticles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdeaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TitleAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    KnowledgeTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentMdAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentMdEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TagsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KnowledgeVisibilityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SourceUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SourceLabelAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SourceLabelEn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeArticles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeArticles_Ideas_IdeaId",
                        column: x => x.IdeaId,
                        principalTable: "Ideas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_KnowledgeArticles_KnowledgeTypes_KnowledgeTypeId",
                        column: x => x.KnowledgeTypeId,
                        principalTable: "KnowledgeTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeArticles_KnowledgeVisibilities_KnowledgeVisibilityId",
                        column: x => x.KnowledgeVisibilityId,
                        principalTable: "KnowledgeVisibilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KnowledgeArticles_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "IpTypes",
                columns: new[] { "Id", "Code", "NameAr", "NameEn", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0010-000000000001"), "patent", "براءة اختراع", "Patent", 1 },
                    { new Guid("00000000-0000-0000-0010-000000000002"), "trademark", "علامة تجارية", "Trademark", 2 },
                    { new Guid("00000000-0000-0000-0010-000000000003"), "copyright", "حقوق النشر", "Copyright", 3 },
                    { new Guid("00000000-0000-0000-0010-000000000004"), "trade_secret", "سر تجاري", "Trade Secret", 4 }
                });

            migrationBuilder.InsertData(
                table: "KnowledgeTypes",
                columns: new[] { "Id", "Code", "NameAr", "NameEn", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0011-000000000001"), "article", "مقال", "Article", 1 },
                    { new Guid("00000000-0000-0000-0011-000000000002"), "case_study", "دراسة حالة", "Case Study", 2 },
                    { new Guid("00000000-0000-0000-0011-000000000003"), "template", "نموذج", "Template", 3 },
                    { new Guid("00000000-0000-0000-0011-000000000004"), "official_guide", "دليل رسمي", "Official Guide", 4 }
                });

            migrationBuilder.InsertData(
                table: "KnowledgeVisibilities",
                columns: new[] { "Id", "Code", "NameAr", "NameEn", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0012-000000000001"), "public", "عام", "Public", 1 },
                    { new Guid("00000000-0000-0000-0012-000000000002"), "internal", "داخلي", "Internal", 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_IpRecords_IdeaId",
                table: "IpRecords",
                column: "IdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_IpRecords_IpTypeId",
                table: "IpRecords",
                column: "IpTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_IpSignatures_IdeaId_UserId",
                table: "IpSignatures",
                columns: new[] { "IdeaId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IpSignatures_UserId",
                table: "IpSignatures",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_IpTypes_Code",
                table: "IpTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeArticles_AuthorId",
                table: "KnowledgeArticles",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeArticles_IdeaId",
                table: "KnowledgeArticles",
                column: "IdeaId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeArticles_KnowledgeTypeId",
                table: "KnowledgeArticles",
                column: "KnowledgeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeArticles_KnowledgeVisibilityId",
                table: "KnowledgeArticles",
                column: "KnowledgeVisibilityId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeTypes_Code",
                table: "KnowledgeTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeVisibilities_Code",
                table: "KnowledgeVisibilities",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IpRecords");

            migrationBuilder.DropTable(
                name: "IpSignatures");

            migrationBuilder.DropTable(
                name: "KnowledgeArticles");

            migrationBuilder.DropTable(
                name: "IpTypes");

            migrationBuilder.DropTable(
                name: "KnowledgeTypes");

            migrationBuilder.DropTable(
                name: "KnowledgeVisibilities");
        }
    }
}
