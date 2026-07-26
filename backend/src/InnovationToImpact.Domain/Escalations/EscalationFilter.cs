namespace InnovationToImpact.Domain.Escalations;

public sealed record EscalationFilter(string? Status, string? Tier, string? EntityType);
