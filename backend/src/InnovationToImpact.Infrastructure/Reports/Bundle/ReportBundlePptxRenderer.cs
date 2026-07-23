using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using InnovationToImpact.Domain.Reports.Bundle;
using A = DocumentFormat.OpenXml.Drawing;

namespace InnovationToImpact.Infrastructure.Reports.Bundle;

/// <summary>
/// Renders a <see cref="ReportBundle"/> (KPIs + tabular sections) into a PowerPoint (.pptx)
/// summary deck: a title slide (localized via <see cref="ReportCatalog.Meta"/>, falling back
/// to <see cref="ReportBundle.Type"/>) with the KPIs as lines, and one slide per section with
/// its rows rendered as text lines (capped, since this is a summary deck rather than a full
/// data dump). Locale-aware (En/Ar) for labels. The OpenXML minimal-presentation scaffolding
/// (presentation/master/layout/theme wiring and slide XML) is copied verbatim from
/// <see cref="AnalyticsReportGenerator"/> since it is fiddly and low-risk to duplicate.
/// </summary>
public class ReportBundlePptxRenderer : IReportBundlePptxRenderer
{
    private const int MaxRowsPerSectionSlide = 12;

    public byte[] Render(ReportBundle bundle, string locale)
    {
        var isAr = locale == "ar";
        var meta = ReportCatalog.Meta(bundle.Type);
        var title = meta is { } m ? (isAr ? m.NameAr : m.NameEn) : bundle.Type;

        var titleLines = bundle.Kpis
            .Select(kpi => $"{(isAr ? kpi.LabelAr : kpi.LabelEn)}: {kpi.Value}")
            .ToArray();

        var slides = new List<(string Title, string[] Lines)>
        {
            (title, titleLines),
        };

        foreach (var section in bundle.Sections)
        {
            var sectionTitle = isAr ? section.TitleAr : section.TitleEn;
            var headerLine = string.Join("  |  ", section.Columns.Select(c => isAr ? c.LabelAr : c.LabelEn));

            var rowLines = section.Rows
                .Take(MaxRowsPerSectionSlide)
                .Select(row => string.Join("  |  ", section.Columns.Select(c => row.TryGetValue(c.Key, out var v) ? v : string.Empty)));

            var lines = new List<string> { headerLine };
            lines.AddRange(rowLines);

            if (section.Rows.Count > MaxRowsPerSectionSlide)
            {
                lines.Add($"... {section.Rows.Count - MaxRowsPerSectionSlide} more row(s) not shown.");
            }

            slides.Add((sectionTitle, lines.ToArray()));
        }

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
