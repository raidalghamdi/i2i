using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovationToImpact.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedTermsContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "CmsContents",
                columns: new[] { "Id", "BodyAr", "BodyEn", "IsPublished", "PublishedAt", "Slug", "TitleAr", "TitleEn", "UpdatedAt" },
                values: new object[] { new Guid("00000000-0000-0000-0034-000000000001"), "باستخدامك منصة «من الابتكار إلى الأثر» فإنك توافق على هذه الشروط والأحكام.\n\nأنت مسؤول عن دقة المعلومات التي تقدّمها وعن امتلاكك الحق في مشاركتها.\n\nتخضع الأفكار المقدَّمة لشروط الملكية الفكرية التي تُقرّها عند التقديم.\n\nيحق للهيئة قبول أي فكرة أو طلب تعديلها أو رفضها أو إحالتها وفق معايير التقييم المنشورة.\n\nتُقدَّم المنصة «كما هي»، ويجوز للهيئة تحديث هذه الشروط وتفاصيل البرنامج من حين لآخر.", "By using the Innovation to Impact platform you agree to these Terms & Conditions.\n\nYou are responsible for the accuracy of the information you submit and for ensuring you have the right to share it.\n\nSubmitted ideas are subject to the intellectual-property terms acknowledged at submission time.\n\nThe Authority may accept, request revision of, reject, or escalate any submission at its discretion based on the published evaluation criteria.\n\nThe platform is provided on an \"as is\" basis; the Authority may update these terms and program details from time to time.", true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "terms", "الشروط والأحكام", "Terms & Conditions", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "CmsContents",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0034-000000000001"));
        }
    }
}
