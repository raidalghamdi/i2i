namespace InnovationToImpact.Domain.SubmitterReview;

public enum SubmitterReviewCommandStatus
{
    Success,
    NotFound,
    Forbidden,
    InvalidState,
    CommentRequired,
    BelowTarget,
}
