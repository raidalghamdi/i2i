using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class ReportTitleConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public ReportTitleConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SavesReportTitleForAUniqueKey()
    {
        Guid reportTitleId;

        using (var context = _fixture.CreateContext())
        {
            var reportTitle = new ReportTitle
            {
                Id = Guid.NewGuid(),
                Key = "idea-funnel-t4a",
                TitleAr = "قمع الأفكار",
                TitleEn = "Idea Funnel",
                SortOrder = 1,
            };
            reportTitleId = reportTitle.Id;

            context.ReportTitles.Add(reportTitle);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var reportTitle = context.ReportTitles.Single(r => r.Id == reportTitleId);
            Assert.Equal("Idea Funnel", reportTitle.TitleEn);
            Assert.Equal(1, reportTitle.SortOrder);
        }
    }

    [Fact]
    public void RejectsDuplicateReportTitleKey()
    {
        using var context = _fixture.CreateContext();

        context.ReportTitles.Add(new ReportTitle { Id = Guid.NewGuid(), Key = "dup-key-t4b", TitleAr = "أ", TitleEn = "A", SortOrder = 1 });
        context.SaveChanges();

        context.ReportTitles.Add(new ReportTitle { Id = Guid.NewGuid(), Key = "dup-key-t4b", TitleAr = "ب", TitleEn = "B", SortOrder = 2 });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
