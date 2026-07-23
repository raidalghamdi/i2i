using InnovationToImpact.Domain.Ideas;

namespace InnovationToImpact.Infrastructure.Tests;

public class IdeaJourneyCalculatorTests
{
    private static readonly IReadOnlyList<JourneyAssignmentInput> NoAssignments = Array.Empty<JourneyAssignmentInput>();
    private static readonly IReadOnlyList<JourneyEvaluationInput> NoEvaluations = Array.Empty<JourneyEvaluationInput>();
    private static readonly IReadOnlyList<JourneyCommitteeDecisionInput> NoDecisions = Array.Empty<JourneyCommitteeDecisionInput>();

    private static IdeaJourney Compute(string status,
        IReadOnlyList<JourneyAssignmentInput>? a = null,
        IReadOnlyList<JourneyEvaluationInput>? e = null,
        IReadOnlyList<JourneyCommitteeDecisionInput>? c = null,
        double? passThreshold = null)
        => passThreshold is null
            ? IdeaJourneyCalculator.Compute(
                new JourneyIdeaInput(status, null, DateTime.UtcNow, DateTime.UtcNow),
                a ?? NoAssignments, e ?? NoEvaluations, c ?? NoDecisions)
            : IdeaJourneyCalculator.Compute(
                new JourneyIdeaInput(status, null, DateTime.UtcNow, DateTime.UtcNow),
                a ?? NoAssignments, e ?? NoEvaluations, c ?? NoDecisions, passThreshold.Value);

    [Fact]
    public void EightStagesWithCanonicalLabels()
    {
        var j = Compute("draft");
        Assert.Equal(8, j.Stages.Count);
        Assert.Equal("Idea Submission", j.Stages[0].Label.En);
        Assert.Equal("Scale & Adoption", j.Stages[7].Label.En);
        Assert.Equal("التقييم الفني", j.Stages[2].Label.Ar);
    }

    [Fact]
    public void Draft_SitsAtSubmission()
    {
        var j = Compute("draft");
        Assert.Equal(StageState.Current, j.Stages[0].State);
        Assert.Equal(StageState.Upcoming, j.Stages[1].State);
        Assert.False(j.Stopped);
    }

    [Fact]
    public void Submitted_Stage1Completed_Stage2Current()
    {
        var j = Compute("submitted");
        Assert.Equal(StageState.Completed, j.Stages[0].State);
        Assert.Equal(StageState.Current, j.Stages[1].State);
    }

    [Fact]
    public void ScreeningRejected_Stage2Stopped()
    {
        var j = Compute("rejected");
        Assert.Equal(StageState.Completed, j.Stages[0].State);
        Assert.Equal(StageState.Stopped, j.Stages[1].State);
        Assert.True(j.Stopped);
    }

    [Fact]
    public void Assigned_Stage3Current()
    {
        var j = Compute("evaluation", a: new[] { new JourneyAssignmentInput(DateTime.UtcNow) });
        Assert.Equal(StageState.Completed, j.Stages[1].State);
        Assert.Equal(StageState.Current, j.Stages[2].State);
    }

    [Fact]
    public void EvaluationFailed_Stage3Stopped()
    {
        var j = Compute("evaluation_failed");
        Assert.Equal(StageState.Stopped, j.Stages[2].State);
        Assert.True(j.Stopped);
    }

    [Fact]
    public void PassAwaitingAttachments_Stage4Current()
    {
        var j = Compute("pass_awaiting_attachments");
        Assert.Equal(StageState.Completed, j.Stages[2].State);
        Assert.Equal(StageState.Current, j.Stages[3].State);
    }

    [Fact]
    public void Committee_Stage4Current()
    {
        var j = Compute("committee");
        Assert.Equal(StageState.Current, j.Stages[3].State);
    }

    [Fact]
    public void Approved_Stage5Completed_PilotCurrent()
    {
        var j = Compute("approved");
        Assert.Equal(StageState.Completed, j.Stages[4].State);
        Assert.Equal(StageState.Current, j.Stages[5].State);
    }

    [Fact]
    public void Closed_AllCompleted()
    {
        var j = Compute("closed");
        Assert.All(j.Stages, s => Assert.Equal(StageState.Completed, s.State));
    }

    [Fact]
    public void EvaluationScore_AveragesSubmittedEvaluations()
    {
        var j = Compute("committee", e: new[]
        {
            new JourneyEvaluationInput(6, DateTime.UtcNow),
            new JourneyEvaluationInput(8, DateTime.UtcNow),
        });
        Assert.Equal(7, j.EvaluationScore);
    }

    [Fact]
    public void NotSelected_Stage5Stopped()
    {
        var j = Compute("not_selected");
        Assert.Equal(StageState.Completed, j.Stages[3].State);
        Assert.Equal(StageState.Stopped, j.Stages[4].State);
        Assert.True(j.Stopped);
    }

    [Fact]
    public void Committee_WithFailingAverage_StatusStillWins_NotStopped()
    {
        // Average is 5.0 (< PassThreshold 6.0), so the score-fallback `failed` branch
        // is true. The explicit `status == "committee"` branch must still be checked
        // first, so the idea sits Current at stage 3 (Committee Review) rather than
        // being force-stopped by the fallback.
        var j = Compute("committee", e: new[]
        {
            new JourneyEvaluationInput(5, DateTime.UtcNow),
            new JourneyEvaluationInput(5, DateTime.UtcNow),
        });
        Assert.Equal(StageState.Current, j.Stages[3].State);
        Assert.False(j.Stopped);
    }

    [Fact]
    public void PassAwaitingAttachments_WithFailingAverage_StatusStillWins_NotStopped()
    {
        // Average is 5.0 (< PassThreshold 6.0), so the score-fallback `failed` branch
        // is true. The explicit `status == "pass_awaiting_attachments"` branch must
        // still be checked first, so the idea sits Current at stage 3 rather than
        // being force-stopped by the fallback.
        var j = Compute("pass_awaiting_attachments", e: new[]
        {
            new JourneyEvaluationInput(5, DateTime.UtcNow),
            new JourneyEvaluationInput(5, DateTime.UtcNow),
        });
        Assert.Equal(StageState.Current, j.Stages[3].State);
        Assert.False(j.Stopped);
    }

    [Fact]
    public void InMeasurement_Stage7Current()
    {
        var j = Compute("in_measurement");
        Assert.Equal(StageState.Completed, j.Stages[5].State); // Pilot done
        Assert.Equal(StageState.Current, j.Stages[6].State);   // Measurement current
        Assert.Equal(StageState.Upcoming, j.Stages[7].State);  // Scale upcoming
    }

    [Fact]
    public void InScaling_Stage8Current()
    {
        var j = Compute("in_scaling");
        Assert.Equal(StageState.Completed, j.Stages[6].State); // Measurement done
        Assert.Equal(StageState.Current, j.Stages[7].State);   // Scale current
    }

    [Fact]
    public void Compute_UsesInjectedThreshold()
    {
        var evaluations = new[]
        {
            new JourneyEvaluationInput(6, DateTime.UtcNow),
            new JourneyEvaluationInput(7, DateTime.UtcNow),
        };
        var assignments = new[] { new JourneyAssignmentInput(DateTime.UtcNow) };

        // Ambiguous status ("evaluation" + an assignment) so the score fallback governs,
        // not an explicit status branch.
        var withHigherThreshold = Compute("evaluation", a: assignments, e: evaluations, passThreshold: 7.0);
        Assert.Equal(StageState.Stopped, withHigherThreshold.Stages[2].State);
        Assert.True(withHigherThreshold.Stopped);

        var withDefaultThreshold = Compute("evaluation", a: assignments, e: evaluations);
        Assert.Equal(StageState.Current, withDefaultThreshold.Stages[3].State);
        Assert.False(withDefaultThreshold.Stopped);
    }
}
