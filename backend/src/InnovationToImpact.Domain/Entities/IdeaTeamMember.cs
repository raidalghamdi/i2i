namespace InnovationToImpact.Domain.Entities;

public class IdeaTeamMember
{
    public Guid Id { get; set; }
    public Guid IdeaId { get; set; }
    public Idea Idea { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
