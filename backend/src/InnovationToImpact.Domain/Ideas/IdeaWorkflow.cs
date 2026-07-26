namespace InnovationToImpact.Domain.Ideas;

/// <summary>Single source of truth for allowed idea status transitions.</summary>
public static class IdeaWorkflow
{
    private static readonly IReadOnlyDictionary<string, string[]> Edges = new Dictionary<string, string[]>
    {
        [IdeaStatusCodes.Draft] = new[] { IdeaStatusCodes.Submitted, IdeaStatusCodes.Withdrawn },
        [IdeaStatusCodes.Submitted] = new[] { IdeaStatusCodes.Evaluation, IdeaStatusCodes.Returned, IdeaStatusCodes.Rejected, IdeaStatusCodes.Withdrawn },
        [IdeaStatusCodes.Returned] = new[] { IdeaStatusCodes.Submitted, IdeaStatusCodes.Withdrawn },
        [IdeaStatusCodes.Evaluation] = new[] { IdeaStatusCodes.EvaluationReview },
        [IdeaStatusCodes.EvaluationReview] = new[] { IdeaStatusCodes.SubmitterReview, IdeaStatusCodes.Returned, IdeaStatusCodes.EvaluationFailed },
        [IdeaStatusCodes.SubmitterReview] = new[] { IdeaStatusCodes.CommitteePending, IdeaStatusCodes.Returned },
        [IdeaStatusCodes.CommitteePending] = new[] { IdeaStatusCodes.Committee },
        [IdeaStatusCodes.Committee] = new[] { IdeaStatusCodes.PendingFinalRanking },
        [IdeaStatusCodes.PendingFinalRanking] = new[] { IdeaStatusCodes.Approved, IdeaStatusCodes.NotSelected },
        [IdeaStatusCodes.Approved] = new[] { IdeaStatusCodes.InPilot },
        [IdeaStatusCodes.InPilot] = new[] { IdeaStatusCodes.InMeasurement },
        [IdeaStatusCodes.InMeasurement] = new[] { IdeaStatusCodes.InScaling },
    };

    public static IReadOnlyCollection<string> AllowedNext(string fromCode) =>
        Edges.TryGetValue(fromCode, out var next) ? next : Array.Empty<string>();

    public static bool CanTransition(string fromCode, string toCode) =>
        Edges.TryGetValue(fromCode, out var next) && Array.IndexOf(next, toCode) >= 0;
}
