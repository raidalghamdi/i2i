using InnovationToImpact.Domain.Reports.Bundle;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InnovationToImpact.Infrastructure.Reports.Bundle;

/// <summary>
/// Renders a <see cref="ReportBundle"/> (KPIs + tabular sections) into a PDF document:
/// a title header (localized via <see cref="ReportCatalog.Meta"/>, falling back to
/// <see cref="ReportBundle.Type"/>), a KPI summary block, and one table per section
/// with a header row of column labels and one data row per section row. Locale-aware
/// (En/Ar) for labels. Adapted from <see cref="AnalyticsReportGenerator"/>'s QuestPDF
/// idiom (margins, header/content/footer structure, table styling).
/// </summary>
public class ReportBundlePdfRenderer : IReportBundlePdfRenderer
{
    public byte[] Render(ReportBundle bundle, string locale)
    {
        var isAr = locale == "ar";
        var meta = ReportCatalog.Meta(bundle.Type);
        var title = meta is { } m ? (isAr ? m.NameAr : m.NameEn) : bundle.Type;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                if (isAr)
                {
                    page.ContentFromRightToLeft();
                }

                page.Header().Text(title).FontSize(20).SemiBold();

                page.Content().Column(column =>
                {
                    column.Spacing(14);

                    foreach (var kpi in bundle.Kpis)
                    {
                        var label = isAr ? kpi.LabelAr : kpi.LabelEn;
                        column.Item().Text($"{label}: {kpi.Value}");
                    }

                    foreach (var section in bundle.Sections)
                    {
                        var sectionTitle = isAr ? section.TitleAr : section.TitleEn;
                        column.Item().Text(sectionTitle).FontSize(14).SemiBold();

                        var headers = section.Columns.Select(c => isAr ? c.LabelAr : c.LabelEn).ToArray();
                        var rows = section.Rows.Select(row =>
                            section.Columns
                                .Select(c => row.TryGetValue(c.Key, out var value) ? value : string.Empty)
                                .ToArray());

                        column.Item().Element(c => RenderTable(c, headers, rows));
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void RenderTable(IContainer container, string[] headers, IEnumerable<string[]> rows)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                foreach (var _ in headers)
                {
                    columns.RelativeColumn();
                }
            });

            table.Header(header =>
            {
                foreach (var heading in headers)
                {
                    header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(heading).SemiBold();
                }
            });

            var hasRows = false;
            foreach (var row in rows)
            {
                hasRows = true;
                foreach (var cell in row)
                {
                    table.Cell().Padding(4).Text(cell);
                }
            }

            if (!hasRows)
            {
                table.Cell().ColumnSpan((uint)headers.Length).Padding(4).Text("No data.").Italic();
            }
        });
    }
}
