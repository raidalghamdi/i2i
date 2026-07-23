using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Phases;

public interface IPhaseScheduleService
{
    Task<IReadOnlyList<PhaseSchedule>> ListAsync(CancellationToken cancellationToken = default);
    Task<PhaseScheduleCommandResult> UpdateAsync(int idx, DateTime? startsAt, DateTime? endsAt, Guid updatedBy, CancellationToken cancellationToken = default);
}
