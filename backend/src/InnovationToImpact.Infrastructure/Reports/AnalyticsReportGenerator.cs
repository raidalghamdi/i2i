using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using InnovationToImpact.Domain.Analytics;
using InnovationToImpact.Domain.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using A = DocumentFormat.OpenXml.Drawing;

namespace InnovationToImpact.Infrastructure.Reports;

public class AnalyticsReportGenerator : IAnalyticsReportGenerator
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsReportGenerator(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    public Task<byte[]> GenerateAsync(string format, CancellationToken cancellationToken = default) => format switch
    {
        ReportFormatCodes.Xlsx => GenerateXlsxAsync(cancellationToken),
        ReportFormatCodes.Pdf => GeneratePdfAsync(cancellationToken),
        ReportFormatCodes.Pptx => GeneratePptxAsync(cancellationToken),
        _ => throw new NotSupportedException($"Analytics export format '{format}' is not supported yet."),
    };

    private async Task<byte[]> GenerateXlsxAsync(CancellationToken cancellationToken)
    {
        var kpis = await _analyticsService.GetExtendedPlatformKpisAsync(cancellationToken);
        var funnel = await _analyticsService.GetFunnelAsync(cancellationToken);
        var cohort = await _analyticsService.GetCohortAsync(cancellationToken);
        var ideasByStage = await _analyticsService.GetIdeasByStageAsync(cancellationToken);
        var topObjectives = await _analyticsService.GetTopObjectivesAsync(cancellationToken);
        var avgTimePerStage = await _analyticsService.GetAvgTimePerStageAsync(cancellationToken);
        var conversion = await _analyticsService.GetConversionAsync(cancellationToken);

        using var workbook = new XLWorkbook();

        var kpisSheet = workbook.Worksheets.Add("KPIs");
        kpisSheet.Cell(1, 1).Value = "Label";
        kpisSheet.Cell(1, 2).Value = "Value";
        var kpiRows = new (string Label, object Value)[]
        {
            ("TotalSubmissions", kpis.TotalSubmissions),
            ("TotalApproved", kpis.TotalApproved),
            ("TotalImplemented", kpis.TotalImplemented),
            ("ActiveSubmitters", kpis.ActiveSubmitters),
            ("TotalEvaluations", kpis.TotalEvaluations),
            ("TotalUsers", kpis.TotalUsers),
            ("TotalEvaluators", kpis.TotalEvaluators),
            ("RealizedFinancialImpact", kpis.RealizedFinancialImpact),
        };
        var kpiRow = 2;
        foreach (var (label, value) in kpiRows)
        {
            kpisSheet.Cell(kpiRow, 1).Value = label;
            kpisSheet.Cell(kpiRow, 2).Value = value switch
            {
                decimal d => (double)d,
                int i => i,
                _ => value.ToString(),
            };
            kpiRow++;
        }

        var funnelSheet = workbook.Worksheets.Add("Funnel");
        funnelSheet.Cell(1, 1).Value = "StageKey";
        funnelSheet.Cell(1, 2).Value = "Count";
        var funnelRow = 2;
        foreach (var entry in funnel)
        {
            funnelSheet.Cell(funnelRow, 1).Value = entry.StageKey;
            funnelSheet.Cell(funnelRow, 2).Value = entry.Count;
            funnelRow++;
        }

        var cohortSheet = workbook.Worksheets.Add("Cohort");
        cohortSheet.Cell(1, 1).Value = "Month";
        cohortSheet.Cell(1, 2).Value = "Submitted";
        cohortSheet.Cell(1, 3).Value = "Approved";
        cohortSheet.Cell(1, 4).Value = "Rejected";
        cohortSheet.Cell(1, 5).Value = "Implemented";
        var cohortRow = 2;
        foreach (var entry in cohort)
        {
            cohortSheet.Cell(cohortRow, 1).Value = entry.Month;
            cohortSheet.Cell(cohortRow, 2).Value = entry.Submitted;
            cohortSheet.Cell(cohortRow, 3).Value = entry.Approved;
            cohortSheet.Cell(cohortRow, 4).Value = entry.Rejected;
            cohortSheet.Cell(cohortRow, 5).Value = entry.Implemented;
            cohortRow++;
        }

        var ideasByStageSheet = workbook.Worksheets.Add("IdeasByStage");
        ideasByStageSheet.Cell(1, 1).Value = "Stage";
        ideasByStageSheet.Cell(1, 2).Value = "Count";
        var ideasByStageRow = 2;
        foreach (var entry in ideasByStage)
        {
            ideasByStageSheet.Cell(ideasByStageRow, 1).Value = entry.Stage;
            ideasByStageSheet.Cell(ideasByStageRow, 2).Value = entry.Count;
            ideasByStageRow++;
        }

        var topObjectivesSheet = workbook.Worksheets.Add("TopObjectives");
        topObjectivesSheet.Cell(1, 1).Value = "NameEn";
        topObjectivesSheet.Cell(1, 2).Value = "Count";
        var topObjectivesRow = 2;
        foreach (var entry in topObjectives)
        {
            topObjectivesSheet.Cell(topObjectivesRow, 1).Value = entry.NameEn;
            topObjectivesSheet.Cell(topObjectivesRow, 2).Value = entry.Count;
            topObjectivesRow++;
        }

        var avgTimePerStageSheet = workbook.Worksheets.Add("AvgTimePerStage");
        avgTimePerStageSheet.Cell(1, 1).Value = "Stage";
        avgTimePerStageSheet.Cell(1, 2).Value = "AvgDays";
        var avgTimePerStageRow = 2;
        foreach (var entry in avgTimePerStage)
        {
            avgTimePerStageSheet.Cell(avgTimePerStageRow, 1).Value = entry.Stage;
            avgTimePerStageSheet.Cell(avgTimePerStageRow, 2).Value = entry.AvgDays;
            avgTimePerStageRow++;
        }

        var conversionSheet = workbook.Worksheets.Add("Conversion");
        conversionSheet.Cell(1, 1).Value = "Submitted";
        conversionSheet.Cell(1, 2).Value = "Pilot";
        conversionSheet.Cell(1, 3).Value = "Rate";
        conversionSheet.Cell(2, 1).Value = conversion.Submitted;
        conversionSheet.Cell(2, 2).Value = conversion.Pilot;
        conversionSheet.Cell(2, 3).Value = conversion.Rate;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private async Task<byte[]> GeneratePdfAsync(CancellationToken cancellationToken)
    {
        var kpis = await _analyticsService.GetExtendedPlatformKpisAsync(cancellationToken);
        var funnel = await _analyticsService.GetFunnelAsync(cancellationToken);
        var cohort = await _analyticsService.GetCohortAsync(cancellationToken);
        var ideasByStage = await _analyticsService.GetIdeasByStageAsync(cancellationToken);
        var topObjectives = await _analyticsService.GetTopObjectivesAsync(cancellationToken);
        var avgTimePerStage = await _analyticsService.GetAvgTimePerStageAsync(cancellationToken);
        var conversion = await _analyticsService.GetConversionAsync(cancellationToken);

        var kpiRows = new[]
        {
            new[] { "Total Submissions", kpis.TotalSubmissions.ToString() },
            new[] { "Total Approved", kpis.TotalApproved.ToString() },
            new[] { "Total Implemented", kpis.TotalImplemented.ToString() },
            new[] { "Active Submitters", kpis.ActiveSubmitters.ToString() },
            new[] { "Total Evaluations", kpis.TotalEvaluations.ToString() },
            new[] { "Total Users", kpis.TotalUsers.ToString() },
            new[] { "Total Evaluators", kpis.TotalEvaluators.ToString() },
            new[] { "Realized Financial Impact", kpis.RealizedFinancialImpact.ToString() },
        };

        var funnelRows = funnel.Select(f => new[] { f.StageKey, f.Count.ToString() });
        var ideasByStageRows = ideasByStage.Select(i => new[] { i.Stage.ToString(), i.Count.ToString() });
        var cohortRows = cohort.Select(c => new[] { c.Month, c.Submitted.ToString(), c.Approved.ToString(), c.Rejected.ToString(), c.Implemented.ToString() });
        var topObjectivesRows = topObjectives.Select(o => new[] { o.NameEn, o.Count.ToString() });
        var avgTimePerStageRows = avgTimePerStage.Select(a => new[] { a.Stage.ToString(), a.AvgDays.ToString() });
        var conversionRows = new[] { new[] { conversion.Submitted.ToString(), conversion.Pilot.ToString(), conversion.Rate.ToString() } };

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Text("Executive Analytics").FontSize(20).SemiBold();

                page.Content().Column(column =>
                {
                    column.Spacing(14);

                    column.Item().Text("Key Performance Indicators").FontSize(14).SemiBold();
                    column.Item().Element(c => RenderTable(c, new[] { "Metric", "Value" }, kpiRows));

                    column.Item().Text("Funnel").FontSize(14).SemiBold();
                    column.Item().Element(c => RenderTable(c, new[] { "Stage", "Count" }, funnelRows));

                    column.Item().Text("Ideas By Stage").FontSize(14).SemiBold();
                    column.Item().Element(c => RenderTable(c, new[] { "Stage", "Count" }, ideasByStageRows));

                    column.Item().Text("Cohort").FontSize(14).SemiBold();
                    column.Item().Element(c => RenderTable(c, new[] { "Month", "Submitted", "Approved", "Rejected", "Implemented" }, cohortRows));

                    column.Item().Text("Top Objectives").FontSize(14).SemiBold();
                    column.Item().Element(c => RenderTable(c, new[] { "Objective", "Count" }, topObjectivesRows));

                    column.Item().Text("Average Time Per Stage").FontSize(14).SemiBold();
                    column.Item().Element(c => RenderTable(c, new[] { "Stage", "Avg Days" }, avgTimePerStageRows));

                    column.Item().Text("Conversion").FontSize(14).SemiBold();
                    column.Item().Element(c => RenderTable(c, new[] { "Submitted", "Pilot", "Rate" }, conversionRows));
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

    private async Task<byte[]> GeneratePptxAsync(CancellationToken cancellationToken)
    {
        var kpis = await _analyticsService.GetExtendedPlatformKpisAsync(cancellationToken);
        var funnel = await _analyticsService.GetFunnelAsync(cancellationToken);
        var cohort = await _analyticsService.GetCohortAsync(cancellationToken);
        var ideasByStage = await _analyticsService.GetIdeasByStageAsync(cancellationToken);
        var topObjectives = await _analyticsService.GetTopObjectivesAsync(cancellationToken);
        var avgTimePerStage = await _analyticsService.GetAvgTimePerStageAsync(cancellationToken);
        var conversion = await _analyticsService.GetConversionAsync(cancellationToken);

        var kpiLines = new[]
        {
            $"Total Submissions: {kpis.TotalSubmissions}",
            $"Total Approved: {kpis.TotalApproved}",
            $"Total Implemented: {kpis.TotalImplemented}",
            $"Active Submitters: {kpis.ActiveSubmitters}",
            $"Total Evaluations: {kpis.TotalEvaluations}",
            $"Total Users: {kpis.TotalUsers}",
            $"Total Evaluators: {kpis.TotalEvaluators}",
            $"Realized Financial Impact: {kpis.RealizedFinancialImpact}",
        };

        var funnelLines = funnel.Select(f => $"{f.StageKey}: {f.Count}").ToArray();
        var cohortLines = cohort
            .Select(c => $"{c.Month}: Submitted={c.Submitted}, Approved={c.Approved}, Rejected={c.Rejected}, Implemented={c.Implemented}")
            .ToArray();
        var ideasByStageLines = ideasByStage.Select(i => $"Stage {i.Stage}: {i.Count}").ToArray();
        var topObjectivesLines = topObjectives.Select(o => $"{o.NameEn}: {o.Count}").ToArray();
        var avgTimePerStageLines = avgTimePerStage.Select(a => $"Stage {a.Stage}: {a.AvgDays.ToString(CultureInfo.InvariantCulture)} days").ToArray();
        var conversionLines = new[] { $"Submitted: {conversion.Submitted}, Pilot: {conversion.Pilot}, Rate: {conversion.Rate.ToString(CultureInfo.InvariantCulture)}" };

        var slides = new (string Title, string[] Lines)[]
        {
            ("Executive Analytics", new[] { "Analytics executive report", "Generated by Innovation to Impact platform" }),
            ("Key Performance Indicators", kpiLines),
            ("Funnel", funnelLines),
            ("Cohort", cohortLines),
            ("Ideas By Stage", ideasByStageLines),
            ("Top Objectives", topObjectivesLines),
            ("Average Time Per Stage", avgTimePerStageLines),
            ("Conversion", conversionLines),
        };

        using var stream = new MemoryStream();
        using (var document = PresentationDocument.Create(stream, PresentationDocumentType.Presentation))
        {
            var presentationPart = document.AddPresentationPart();
            presentationPart.Presentation = new Presentation();

            var slideMasterPart = presentationPart.AddNewPart<SlideMasterPart>();

            var themePart = slideMasterPart.AddNewPart<ThemePart>();
            themePart.Theme = CreateMinimalTheme();
            themePart.Theme.Save();

            var slideLayoutPart = slideMasterPart.AddNewPart<SlideLayoutPart>();
            slideLayoutPart.SlideLayout = CreateMinimalSlideLayout();
            slideLayoutPart.SlideLayout.Save();
            slideLayoutPart.AddPart(slideMasterPart);

            var layoutRelationshipId = slideMasterPart.GetIdOfPart(slideLayoutPart);
            slideMasterPart.SlideMaster = CreateMinimalSlideMaster(layoutRelationshipId);
            slideMasterPart.SlideMaster.Save();

            var masterRelationshipId = presentationPart.GetIdOfPart(slideMasterPart);

            var slideIdList = new SlideIdList();
            uint nextSlideId = 256;

            foreach (var slide in slides)
            {
                var slidePart = presentationPart.AddNewPart<SlidePart>();
                slidePart.AddPart(slideLayoutPart);
                slidePart.Slide = CreateSlide(slide.Title, slide.Lines);
                slidePart.Slide.Save();

                var slideRelationshipId = presentationPart.GetIdOfPart(slidePart);
                slideIdList.Append(new SlideId { Id = nextSlideId++, RelationshipId = slideRelationshipId });
            }

            presentationPart.Presentation.Append(
                new SlideMasterIdList(new SlideMasterId { Id = 2147483648U, RelationshipId = masterRelationshipId }),
                slideIdList,
                new SlideSize { Cx = 9144000, Cy = 6858000, Type = SlideSizeValues.Screen4x3 },
                new NotesSize { Cx = 6858000, Cy = 9144000 });
            presentationPart.Presentation.Save();
        }

        return stream.ToArray();
    }

    private static A.Theme CreateMinimalTheme() => new(
        """
        <a:theme xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" name="Innovation Theme">
          <a:themeElements>
            <a:clrScheme name="Office">
              <a:dk1><a:sysClr val="windowText" lastClr="000000"/></a:dk1>
              <a:lt1><a:sysClr val="window" lastClr="FFFFFF"/></a:lt1>
              <a:dk2><a:srgbClr val="1F497D"/></a:dk2>
              <a:lt2><a:srgbClr val="EEECE1"/></a:lt2>
              <a:accent1><a:srgbClr val="4F81BD"/></a:accent1>
              <a:accent2><a:srgbClr val="C0504D"/></a:accent2>
              <a:accent3><a:srgbClr val="9BBB59"/></a:accent3>
              <a:accent4><a:srgbClr val="8064A2"/></a:accent4>
              <a:accent5><a:srgbClr val="4BACC6"/></a:accent5>
              <a:accent6><a:srgbClr val="F79646"/></a:accent6>
              <a:hlink><a:srgbClr val="0000FF"/></a:hlink>
              <a:folHlink><a:srgbClr val="800080"/></a:folHlink>
            </a:clrScheme>
            <a:fontScheme name="Office">
              <a:majorFont>
                <a:latin typeface="Calibri"/>
                <a:ea typeface=""/>
                <a:cs typeface=""/>
              </a:majorFont>
              <a:minorFont>
                <a:latin typeface="Calibri"/>
                <a:ea typeface=""/>
                <a:cs typeface=""/>
              </a:minorFont>
            </a:fontScheme>
            <a:fmtScheme name="Office">
              <a:fillStyleLst>
                <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
                <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
                <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
              </a:fillStyleLst>
              <a:lnStyleLst>
                <a:ln w="9525"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:ln>
                <a:ln w="25400"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:ln>
                <a:ln w="38100"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:ln>
              </a:lnStyleLst>
              <a:effectStyleLst>
                <a:effectStyle><a:effectLst/></a:effectStyle>
                <a:effectStyle><a:effectLst/></a:effectStyle>
                <a:effectStyle><a:effectLst/></a:effectStyle>
              </a:effectStyleLst>
              <a:bgFillStyleLst>
                <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
                <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
                <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
              </a:bgFillStyleLst>
            </a:fmtScheme>
          </a:themeElements>
        </a:theme>
        """);

    private static SlideLayout CreateMinimalSlideLayout() => new(
        """
        <p:sldLayout xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main" type="blank" preserve="1">
          <p:cSld name="Blank">
            <p:spTree>
              <p:nvGrpSpPr>
                <p:cNvPr id="1" name=""/>
                <p:cNvGrpSpPr/>
                <p:nvPr/>
              </p:nvGrpSpPr>
              <p:grpSpPr/>
            </p:spTree>
          </p:cSld>
          <p:clrMapOvr>
            <a:overrideClrMapping/>
          </p:clrMapOvr>
        </p:sldLayout>
        """);

    private static SlideMaster CreateMinimalSlideMaster(string layoutRelationshipId) => new(
        $"""
        <p:sldMaster xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main">
          <p:cSld>
            <p:spTree>
              <p:nvGrpSpPr>
                <p:cNvPr id="1" name=""/>
                <p:cNvGrpSpPr/>
                <p:nvPr/>
              </p:nvGrpSpPr>
              <p:grpSpPr/>
            </p:spTree>
          </p:cSld>
          <p:clrMap bg1="lt1" tx1="dk1" bg2="lt2" tx2="dk2" accent1="accent1" accent2="accent2" accent3="accent3" accent4="accent4" accent5="accent5" accent6="accent6" hlink="hlink" folHlink="folHlink"/>
          <p:sldLayoutIdLst>
            <p:sldLayoutId id="2147483649" r:id="{layoutRelationshipId}"/>
          </p:sldLayoutIdLst>
        </p:sldMaster>
        """);

    private static Slide CreateSlide(string title, IReadOnlyList<string> lines)
    {
        var contentLines = lines.Count > 0 ? lines : new[] { "No data." };
        var paragraphs = new StringBuilder();
        foreach (var line in contentLines)
        {
            paragraphs.Append("<a:p><a:r><a:t>").Append(EscapeXmlText(line)).Append("</a:t></a:r></a:p>");
        }

        var xml =
            $"""
            <p:sld xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main">
              <p:cSld>
                <p:spTree>
                  <p:nvGrpSpPr>
                    <p:cNvPr id="1" name=""/>
                    <p:cNvGrpSpPr/>
                    <p:nvPr/>
                  </p:nvGrpSpPr>
                  <p:grpSpPr/>
                  <p:sp>
                    <p:nvSpPr>
                      <p:cNvPr id="2" name="Title"/>
                      <p:cNvSpPr><a:spLocks noGrp="1"/></p:cNvSpPr>
                      <p:nvPr><p:ph type="title"/></p:nvPr>
                    </p:nvSpPr>
                    <p:spPr>
                      <a:xfrm><a:off x="457200" y="274638"/><a:ext cx="8229600" cy="1143000"/></a:xfrm>
                      <a:prstGeom prst="rect"><a:avLst/></a:prstGeom>
                    </p:spPr>
                    <p:txBody>
                      <a:bodyPr/>
                      <a:lstStyle/>
                      <a:p><a:r><a:t>{EscapeXmlText(title)}</a:t></a:r></a:p>
                    </p:txBody>
                  </p:sp>
                  <p:sp>
                    <p:nvSpPr>
                      <p:cNvPr id="3" name="Content"/>
                      <p:cNvSpPr><a:spLocks noGrp="1"/></p:cNvSpPr>
                      <p:nvPr><p:ph type="body" idx="1"/></p:nvPr>
                    </p:nvSpPr>
                    <p:spPr>
                      <a:xfrm><a:off x="457200" y="1600200"/><a:ext cx="8229600" cy="4525963"/></a:xfrm>
                      <a:prstGeom prst="rect"><a:avLst/></a:prstGeom>
                    </p:spPr>
                    <p:txBody>
                      <a:bodyPr/>
                      <a:lstStyle/>
                      {paragraphs}
                    </p:txBody>
                  </p:sp>
                </p:spTree>
              </p:cSld>
              <p:clrMapOvr>
                <a:overrideClrMapping/>
              </p:clrMapOvr>
            </p:sld>
            """;

        return new Slide(xml);
    }

    private static string EscapeXmlText(string value) =>
        value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
