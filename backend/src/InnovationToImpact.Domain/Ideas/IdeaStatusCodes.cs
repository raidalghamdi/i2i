namespace InnovationToImpact.Domain.Ideas;

public static class IdeaStatusCodes
{
    public const string Draft = "draft";
    public const string Submitted = "submitted";
    public const string Evaluation = "evaluation";
    public const string PassAwaitingAttachments = "pass_awaiting_attachments";
    public const string EvaluationFailed = "evaluation_failed";
    public const string Committee = "committee";
    public const string PendingFinalRanking = "pending_final_ranking";
    public const string Rejected = "rejected";
    public const string Returned = "returned";
    public const string Approved = "approved";
    public const string NotSelected = "not_selected";
    public const string InPilot = "in_pilot";
    public const string InMeasurement = "in_measurement";
    public const string InScaling = "in_scaling";
    public const string Withdrawn = "withdrawn";
}
