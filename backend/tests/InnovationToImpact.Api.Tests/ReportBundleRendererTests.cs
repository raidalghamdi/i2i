using ClosedXML.Excel;
using InnovationToImpact.Domain.Reports.Bundle;
using InnovationToImpact.Infrastructure.Reports.Bundle;
using Xunit;

namespace InnovationToImpact.Api.Tests;

public class ReportBundleRendererTests
{
    private static ReportBundle BuildSampleBundle()
    {
        var kpis = new List<ReportKpi>
        {
            new("Total", "الإجمالي", "10"),
            new("Approved", "المعتمدة", "4"),
        };

        var columns = new List<ReportColumn>
        {
            new("theme", "Theme", "المسار"),
            new("count", "Count", "العدد"),
        };

        var rows = new List<IReadOnlyDictionary<string, string>>
        {
            new Dictionary<string, string> { ["theme"] = "Digital Innovation", ["count"] = "6" },
            new Dictionary<string, string> { ["theme"] = "Customer Experience" }, // missing "count" on purpose
        };

        var section = new ReportSection("Distribution by Strategic Theme", "التوزيع حسب المسار الاستراتيجي", columns, rows);

        return new ReportBundle(
            ReportTypeCodes.Executive,
            new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc),
            "tester",
            null,
            null,
            kpis,
            new[] { section });
    }

    [Fact]
    public void Render_ProducesSummarySheetWithKpiRows()
    {
        var bundle = BuildSampleBundle();
        var renderer = new ReportBundleXlsxRenderer();

        var bytes = renderer.Render(bundle, "en");

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);

        var summary = workbook.Worksheet("Summary");
        Assert.Equal("Metric", summary.Cell(1, 1).GetString());
        Assert.Equal("Value", summary.Cell(1, 2).GetString());
        Assert.Equal("Total", summary.Cell(2, 1).GetString());
        Assert.Equal("10", summary.Cell(2, 2).GetString());
        Assert.Equal("Approved", summary.Cell(3, 1).GetString());
        Assert.Equal("4", summary.Cell(3, 2).GetString());
    }

    [Fact]
    public void Render_ProducesSectionSheetWithHeaderAndDataRows()
    {
        var bundle = BuildSampleBundle();
        var renderer = new ReportBundleXlsxRenderer();

        var bytes = renderer.Render(bundle, "en");

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);

        var sectionSheet = workbook.Worksheet("Distribution by Strategic Theme");
        Assert.Equal("Theme", sectionSheet.Cell(1, 1).GetString());
        Assert.Equal("Count", sectionSheet.Cell(1, 2).GetString());
        Assert.Equal("Digital Innovation", sectionSheet.Cell(2, 1).GetString());
        Assert.Equal("6", sectionSheet.Cell(2, 2).GetString());
        Assert.Equal("Customer Experience", sectionSheet.Cell(3, 1).GetString());
        Assert.Equal(string.Empty, sectionSheet.Cell(3, 2).GetString()); // missing key -> ""
    }

    [Fact]
    public void Render_ArabicLocale_UsesArabicLabels()
    {
        var bundle = BuildSampleBundle();
        var renderer = new ReportBundleXlsxRenderer();

        var bytes = renderer.Render(bundle, "ar");

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);

        var summary = workbook.Worksheet("Summary");
        Assert.Equal("الإجمالي", summary.Cell(2, 1).GetString());

        var sectionSheet = workbook.Worksheet("Distribution by Strategic Theme");
        Assert.Equal("المسار", sectionSheet.Cell(1, 1).GetString());
        Assert.Equal("العدد", sectionSheet.Cell(1, 2).GetString());
    }
}
