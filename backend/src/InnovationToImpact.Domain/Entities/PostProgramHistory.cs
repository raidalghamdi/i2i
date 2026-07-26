namespace InnovationToImpact.Domain.Entities;

public class PostProgramHistory
{
    public Guid Id { get; set; }

    public Guid IdeaId { get; set; }
    public Idea Idea { get; set; } = null!;

    public string FromStage { get; set; } = string.Empty;
    public string ToStage { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;

    public Guid ChangedById { get; set; }
    public User ChangedBy { get; set; } = null!;

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
