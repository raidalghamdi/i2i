using InnovationToImpact.Domain.Evaluations;

namespace InnovationToImpact.Domain.Ideas;

/// <summary>
/// Derives the eight-stage lifecycle position of an idea from its REAL state
/// (status + assignments + evaluations + committee decisions), not a stored column.
/// Ported from the legacy computeIdeaStage() (transition rules A–K). Two backend
/// statuses absent from legacy — pending_final_ranking and not_selected — are
/// mapped onto the approval stage (5).
/// </summary>
public static class IdeaJourneyCalculator
{
    public static readonly IReadOnlyList<StageLabel> StageLabels = new[]
    {
        new StageLabel("تقديم الفكرة", "Idea Submission"),
        new StageLabel("الفرز الأولي", "Initial Screening"),
        new StageLabel("التقييم الفني", "Technical Evaluation"),
        new StageLabel("مراجعة اللجنة", "Committee Review"),
        new StageLabel("الاعتماد", "Approval"),
        new StageLabel("التنفيذ التجريبي", "Pilot Implementation"),
        new StageLabel("القياس والأثر", "Measurement & Impact"),
        new StageLabel("التوسّع والاعتماد", "Scale & Adoption"),
    };

    public static IdeaJourney Compute(
        JourneyIdeaInput idea,
        IReadOnlyList<JourneyAssignmentInput> assignments,
        IReadOnlyList<JourneyEvaluationInput> evaluations,
        IReadOnlyList<JourneyCommitteeDecisionInput> committeeDecisions,
        double passThreshold = (double)EvaluationScoreRules.PassThreshold)
    {
        var status = (idea.Status ?? "draft").ToLowerInvariant();
        var evaluationScore = ComputeEvaluationScore(evaluations);

        var hasAssignment = assignments.Count > 0;
        var hasEvaluation = evaluations.Count > 0;
        var committeeApprove = committeeDecisions.Any(c => string.Equals(c.Decision, "approve", StringComparison.OrdinalIgnoreCase));
        var committeeReject = committeeDecisions.Any(c => string.Equals(c.Decision, "reject", StringComparison.OrdinalIgnoreCase));
        var passed = evaluationScore != null && evaluationScore.Value >= passThreshold;
        var failed = evaluationScore != null && evaluationScore.Value < passThreshold;

        // 1-based stage positions; 0 = none.
        int completedUpTo = 0, current = 0, stopped = 0;

        if (status is "closed" or "archived") { completedUpTo = 8; }
        else if (status == "in_implementation") { completedUpTo = 7; current = 8; }
        else if (status == "benefits_tracking") { completedUpTo = 6; current = 7; }
        else if (status == "in_pilot") { completedUpTo = 5; current = 6; }
        else if (status == "in_scaling") { completedUpTo = 7; current = 8; }
        else if (status == "in_measurement") { completedUpTo = 6; current = 7; }
        else if (status == "approved" || committeeApprove) { completedUpTo = 5; current = 6; }
        else if (status == "pending_final_ranking") { completedUpTo = 4; current = 5; }
        else if (status == "not_selected") { completedUpTo = 4; stopped = 5; }
        else if (status == "withdrawn")
        {
            var active = 2;
            if (committeeApprove) active = 5;
            else if (passed) active = 4;
            else if (hasEvaluation || hasAssignment) active = 3;
            stopped = active;
            completedUpTo = active - 1;
        }
        else if (committeeReject && status == "rejected") { completedUpTo = 3; stopped = 4; }
        else if (status == "rejected") { completedUpTo = 1; stopped = 2; }
        else if (status == "committee") { completedUpTo = 3; current = 4; }
        else if (status == "pass_awaiting_attachments") { completedUpTo = 3; current = 4; }
        else if (status == "evaluation_failed") { completedUpTo = 2; stopped = 3; }
        else if (failed) { completedUpTo = 2; stopped = 3; }
        else if (passed) { completedUpTo = 3; current = 4; }
        else if (hasAssignment || status is "assigned" or "evaluation") { completedUpTo = 2; current = 3; }
        else if (status == "returned") { completedUpTo = 1; current = 2; }
        else if (status is "submitted" or "screening" or "needs_completion") { completedUpTo = 1; current = 2; }
        else { completedUpTo = 0; current = 1; }

        var submittedAt = idea.SubmittedAt ?? idea.CreatedAt;
        var assignmentAt = Earliest(assignments.Select(a => a.CreatedAt));
        var evaluationAt = Latest(evaluations.Select(e => e.SubmittedAt));
        var approvalAt = Latest(committeeDecisions
            .Where(c => string.Equals(c.Decision, "approve", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.DecidedAt));
        var updatedAt = idea.UpdatedAt;

        DateTime? CompletedAtFor(int index) => index switch
        {
            0 => submittedAt,
            1 => assignmentAt ?? submittedAt,
            2 => assignmentAt,
            3 => evaluationAt,
            4 => approvalAt ?? updatedAt,
            _ => updatedAt,
        };

        var stages = new List<JourneyStage>(StageLabels.Count);
        for (var i = 0; i < StageLabels.Count; i++)
        {
            var oneBased = i + 1;
            StageState state;
            if (oneBased == stopped) state = StageState.Stopped;
            else if (oneBased <= completedUpTo) state = StageState.Completed;
            else if (oneBased == current) state = StageState.Current;
            else state = StageState.Upcoming;

            stages.Add(new JourneyStage(i, state, StageLabels[i],
                state == StageState.Completed ? CompletedAtFor(i) : null));
        }

        var activeOneBased = stopped != 0 ? stopped : current != 0 ? current : completedUpTo;
        return new IdeaJourney(Math.Max(0, activeOneBased - 1), stopped > 0, evaluationScore, stages);
    }

    private static double? ComputeEvaluationScore(IReadOnlyList<JourneyEvaluationInput> evaluations)
    {
        var scores = evaluations.Where(e => e.Score.HasValue).Select(e => e.Score!.Value).ToList();
        return scores.Count == 0 ? null : scores.Average();
    }

    private static DateTime? Earliest(IEnumerable<DateTime?> dates)
    {
        var valid = dates.Where(d => d.HasValue).Select(d => d!.Value).ToList();
        return valid.Count == 0 ? null : valid.Min();
    }

    private static DateTime? Latest(IEnumerable<DateTime?> dates)
    {
        var valid = dates.Where(d => d.HasValue).Select(d => d!.Value).ToList();
        return valid.Count == 0 ? null : valid.Max();
    }
}
