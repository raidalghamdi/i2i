namespace InnovationToImpact.Domain.Ideas;

public static class IdeaSectionKeys
{
    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        "title",
        "problem_statement",
        "proposed_solution",
        "expected_benefits",
        "activity_id",
        "strategic_theme_id",
        "challenge",
        "participation_type",
        "team",
        "attachments",
    };
}
