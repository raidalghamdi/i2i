using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Committee;

public interface ICommitteeService
{
    Task<CommitteeCommandResult> SubmitDecisionAsync(Guid ideaId, Guid judgeId, CommitteeDecisionInput input, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CommitteeQueueItem>> GetQueueAsync(Guid judgeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CommitteeDecision>> GetMyDecisionsAsync(Guid judgeId, CancellationToken cancellationToken = default);
}
