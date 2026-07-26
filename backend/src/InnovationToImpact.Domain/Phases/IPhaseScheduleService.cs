using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Phases;

public interface IPhaseScheduleService
{
    Task<IReadOnlyList<PhaseSchedule>> ListAsync(CancellationToken cancellationToken = default);
    Task<PhaseScheduleCommandResult> UpdateAsync(int idx, DateTime? startsAt, DateTime? endsAt, Guid updatedBy, CancellationToken cancellationToken = default);
    Task<PhaseAnnounceResult> AnnounceAsync(int idx, Guid actorId, CancellationToken cancellationToken = default);

    /// <summary>Role codes configured as the notification audience for the given phase. Empty if none configured.</summary>
    Task<IReadOnlyList<string>> GetAudienceAsync(int idx, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the phase's audience rows with the given role codes. Unknown role codes (not present in the
    /// Roles catalog) are silently skipped rather than rejecting the whole request, so a client sending one
    /// stale/typo'd code doesn't lose the rest of a valid selection.
    /// </summary>
    Task SetAudienceAsync(int idx, IReadOnlyList<string> roleCodes, CancellationToken cancellationToken = default);
}
