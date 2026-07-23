namespace InnovationToImpact.Domain.Ideas;

public enum StageState
{
    Completed,
    Current,
    Stopped,
    Upcoming,
}

public sealed record StageLabel(string Ar, string En);

public sealed record JourneyStage(int Index, StageState State, StageLabel Label, DateTime? CompletedAt);

public sealed record IdeaJourney(int CurrentStage, bool Stopped, double? EvaluationScore, IReadOnlyList<JourneyStage> Stages);

// Minimal, defensive input shapes — only the fields the calculator reads.
public sealed record JourneyIdeaInput(string? Status, DateTime? SubmittedAt, DateTime? CreatedAt, DateTime? UpdatedAt);
public sealed record JourneyAssignmentInput(DateTime? CreatedAt);
public sealed record JourneyEvaluationInput(double? Score, DateTime? SubmittedAt);
public sealed record JourneyCommitteeDecisionInput(string? Decision, DateTime? DecidedAt);
