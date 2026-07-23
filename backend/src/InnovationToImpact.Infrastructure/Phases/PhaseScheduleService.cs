using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Phases;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Phases;

public class PhaseScheduleService : IPhaseScheduleService
{
    private readonly InnovationDbContext _db;

    public PhaseScheduleService(InnovationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PhaseSchedule>> ListAsync(CancellationToken cancellationToken = default) =>
        await _db.PhaseSchedules.OrderBy(p => p.Idx).ToListAsync(cancellationToken);

    public async Task<PhaseScheduleCommandResult> UpdateAsync(int idx, DateTime? startsAt, DateTime? endsAt, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        var phase = await _db.PhaseSchedules.SingleOrDefaultAsync(p => p.Idx == idx, cancellationToken);
        if (phase is null) return new PhaseScheduleCommandResult(PhaseScheduleCommandStatus.NotFound);

        phase.StartsAt = startsAt;
        phase.EndsAt = endsAt;
        phase.UpdatedAt = DateTime.UtcNow;
        phase.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(cancellationToken);
        return new PhaseScheduleCommandResult(PhaseScheduleCommandStatus.Success, phase);
    }
}
