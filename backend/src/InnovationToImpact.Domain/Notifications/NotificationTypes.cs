namespace InnovationToImpact.Domain.Notifications;

public static class NotificationTypes
{
    public const string PhaseAnnounced = "phase_announced";
    public const string EvaluationAssigned = "evaluation_assigned";
    public const string ApprovalRequested = "approval_requested";
    public const string IdeaWithdrawn = "idea_withdrawn";

    // Change 20260726
    public const string IdeaSubmitted = "idea_submitted";
    public const string EvaluationCompleted = "evaluation_completed";
    public const string CommitteeDecisionMade = "committee_decision_made";
    public const string IdeaStatusChanged = "idea_status_changed";
}
