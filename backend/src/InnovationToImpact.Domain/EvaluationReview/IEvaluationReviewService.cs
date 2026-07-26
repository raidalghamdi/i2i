using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.EvaluationReview;

public interface IEvaluationReviewService
{
    Task<EvaluationReviewCommandResult> SubmitDecisionAsync(Guid ideaId, Guid supervisorId, EvaluationReviewDecisionInput input, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Idea>> GetReviewQueueAsync(CancellationToken cancellationToken = default);
}
