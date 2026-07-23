namespace InnovationToImpact.Domain.Reports.Bundle;

public static class ReportTypeCodes
{
    public const string Executive = "executive";
    public const string Detailed = "detailed";
    public const string Media = "media";
    public const string Cx = "cx";
    public const string Operational = "operational";
    public const string Audit = "audit";
    public const string Ideas = "ideas";
    public const string Evaluators = "evaluators";
    public const string Themes = "themes";
    public const string Innovators = "innovators";
    public const string Committee = "committee";
    public const string Trends = "trends";

    public static readonly string[] All =
    {
        Executive, Detailed, Media, Cx, Operational, Audit,
        Ideas, Evaluators, Themes, Innovators, Committee, Trends,
    };
}
