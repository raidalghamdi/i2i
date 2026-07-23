namespace InnovationToImpact.Domain.Reports;

public sealed record ReportTitleInput(string Key, string TitleAr, string TitleEn, int SortOrder);

public sealed record ReportTitlePatch(string TitleAr, string TitleEn, int SortOrder);
