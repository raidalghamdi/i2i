using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.CommitteeReferral;

public interface ICommitteeReferralService
{
    Task<CommitteeReferralCommandResult> SubmitToCommitteeAsync(Guid ideaId, Guid supervisorId, SubmitToCommitteeInput input, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Idea>> GetPendingQueueAsync(CancellationToken cancellationToken = default);
}
