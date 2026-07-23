namespace InnovationToImpact.Domain.Screening;

public sealed record ScreeningDecisionInput(string DecisionCode, string? Reason, IReadOnlyList<string>? EditableSections = null);
