using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Screening;

public interface IScreeningService
{
    Task<ScreeningCommandResult> SubmitDecisionAsync(Guid ideaId, Guid supervisorId, ScreeningDecisionInput input, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Idea>> GetQueueAsync(CancellationToken cancellationToken = default);
}
