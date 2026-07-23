namespace InnovationToImpact.Domain.Ideas;

public static class IdeaSectionKeys
{
    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        "title",
        "proposed_solution",
        "activity_id",
        "strategic_theme_id",
        "challenge",
        "participation_type",
        "team",
        "attachments",
    };
}
