namespace InnovationToImpact.Domain.Reports.Bundle;

public sealed record ReportColumn(string Key, string LabelEn, string LabelAr);

public sealed record ReportKpi(string LabelEn, string LabelAr, string Value);

public sealed record ReportSection(
    string TitleEn,
    string TitleAr,
    IReadOnlyList<ReportColumn> Columns,
    IReadOnlyList<IReadOnlyDictionary<string, string>> Rows);

public sealed record ReportBundle(
    string Type,
    DateTime GeneratedAt,
    string GeneratedBy,
    DateTime? DateFrom,
    DateTime? DateTo,
    IReadOnlyList<ReportKpi> Kpis,
    IReadOnlyList<ReportSection> Sections)
{
    public int TotalRowCount => Sections.Sum(s => s.Rows.Count);
}
