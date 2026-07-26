namespace InnovationToImpact.Domain.Entities;

public class KnowledgeArticle
{
    public Guid Id { get; set; }

    public Guid? IdeaId { get; set; }
    public Idea? Idea { get; set; }

    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;

    public Guid KnowledgeTypeId { get; set; }
    public KnowledgeType KnowledgeType { get; set; } = null!;

    public string ContentMdAr { get; set; } = string.Empty;
    public string ContentMdEn { get; set; } = string.Empty;

    public string? TagsJson { get; set; }

    public Guid KnowledgeVisibilityId { get; set; }
    public KnowledgeVisibility KnowledgeVisibility { get; set; } = null!;

    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public DateTime? PublishedAt { get; set; }
    public string? SourceUrl { get; set; }
    public string? SourceLabelAr { get; set; }
    public string? SourceLabelEn { get; set; }
}
