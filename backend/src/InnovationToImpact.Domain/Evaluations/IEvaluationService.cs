using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Evaluations;

public interface IEvaluationService
{
    Task<EvaluationCommandResult> SubmitAsync(Guid ideaId, Guid evaluatorId, EvaluationInput input, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Idea>> GetQueueAsync(Guid evaluatorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Evaluation>> GetMyEvaluationsAsync(Guid evaluatorId, CancellationToken cancellationToken = default);
}
