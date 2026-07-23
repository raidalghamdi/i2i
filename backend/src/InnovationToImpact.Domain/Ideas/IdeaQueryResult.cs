using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Ideas;

public sealed record IdeaQueryResult(IdeaCommandStatus Status, Idea? Idea = null);
