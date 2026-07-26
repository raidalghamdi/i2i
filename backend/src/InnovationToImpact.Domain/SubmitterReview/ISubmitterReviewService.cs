namespace InnovationToImpact.Domain.SubmitterReview;

public interface ISubmitterReviewService
{
    Task<SubmitterReviewCommandResult> ResubmitAsync(Guid ideaId, Guid submitterId, ResubmitEvaluationInput input, CancellationToken cancellationToken = default);
}
