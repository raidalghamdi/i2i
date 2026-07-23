using System.Text;
using InnovationToImpact.Domain.Reports.Bundle;
using InnovationToImpact.Infrastructure.Reports.Bundle;
using Xunit;

namespace InnovationToImpact.Api.Tests;

public class ReportBundlePdfRendererTests
{
    static ReportBundlePdfRendererTests()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    }

    private static ReportBundle BuildSampleBundle()
    {
        var kpis = new List<ReportKpi>
        {
            new("Total", "الإجمالي", "10"),
            new("Approved", "المعتمدة", "4"),
        };

        var columns = new List<ReportColumn>
        {
            new("a", "AEn", "AAr"),
            new("b", "BEn", "BAr"),
        };

        var rows = new List<IReadOnlyDictionary<string, string>>
        {
            new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" },
            new Dictionary<string, string> { ["a"] = "3", ["b"] = "4" },
        };

        var section = new ReportSection("SecEn", "SecAr", columns, rows);

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
    public void Render_English_ProducesValidPdfBytes()
    {
        var bundle = BuildSampleBundle();
        var renderer = new ReportBundlePdfRenderer();

        var bytes = renderer.Render(bundle, "en");

        Assert.NotEmpty(bytes);
        Assert.True(bytes.Length > 5);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
    }

    [Fact]
    public void Render_Arabic_DoesNotThrowAndProducesNonEmptyBytes()
    {
        var bundle = BuildSampleBundle();
        var renderer = new ReportBundlePdfRenderer();

        var bytes = renderer.Render(bundle, "ar");

        Assert.NotEmpty(bytes);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
    }
}
