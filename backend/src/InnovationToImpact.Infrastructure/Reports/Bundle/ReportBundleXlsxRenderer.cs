using ClosedXML.Excel;
using InnovationToImpact.Domain.Reports.Bundle;
using InnovationToImpact.Infrastructure.Backup;

namespace InnovationToImpact.Infrastructure.Reports.Bundle;

/// <summary>
/// Renders a <see cref="ReportBundle"/> (KPIs + tabular sections) into an XLSX workbook:
/// a "Summary" sheet listing the KPIs, followed by one sheet per section with a header
/// row of column labels and one data row per section row. Locale-aware (En/Ar) for
/// labels; sheet names reuse <see cref="BackupExportService.SafeSheetName"/> for
/// Excel-safe, deduplicated, truncated sheet names.
/// </summary>
public class ReportBundleXlsxRenderer : IReportBundleXlsxRenderer
{
    public byte[] Render(ReportBundle bundle, string locale)
    {
        var isAr = locale == "ar";
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var workbook = new XLWorkbook();

        var summarySheetName = BackupExportService.SafeSheetName("Summary", usedNames);
        var summarySheet = workbook.Worksheets.Add(summarySheetName);
        summarySheet.Cell(1, 1).Value = "Metric";
        summarySheet.Cell(1, 2).Value = "Value";

        var kpiRow = 2;
        foreach (var kpi in bundle.Kpis)
        {
            summarySheet.Cell(kpiRow, 1).Value = isAr ? kpi.LabelAr : kpi.LabelEn;
            summarySheet.Cell(kpiRow, 2).Value = kpi.Value;
            kpiRow++;
        }

        foreach (var section in bundle.Sections)
        {
            var sheetName = BackupExportService.SafeSheetName(section.TitleEn, usedNames);
            var sheet = workbook.Worksheets.Add(sheetName);

            for (var c = 0; c < section.Columns.Count; c++)
            {
                var column = section.Columns[c];
                sheet.Cell(1, c + 1).Value = isAr ? column.LabelAr : column.LabelEn;
            }

            for (var r = 0; r < section.Rows.Count; r++)
            {
                var row = section.Rows[r];
                for (var c = 0; c < section.Columns.Count; c++)
                {
                    var key = section.Columns[c].Key;
                    sheet.Cell(r + 2, c + 1).Value = row.TryGetValue(key, out var value) ? value : "";
                }
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
