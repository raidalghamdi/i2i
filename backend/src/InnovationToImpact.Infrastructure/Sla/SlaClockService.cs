using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Sla;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Sla;

public class SlaClockService : ISlaClockService
{
    private readonly InnovationDbContext _db;

    public SlaClockService(InnovationDbContext db)
    {
        _db = db;
    }

    public async Task OpenAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        var policy = await _db.SlaPolicies.FirstOrDefaultAsync(p => p.EntityType == entityType, cancellationToken);
        if (policy is null) return;

        var alreadyOpen = await _db.SlaTrackings.AnyAsync(
            t => t.SlaPolicyId == policy.Id && t.EntityId == entityId && t.ResolvedAt == null,
            cancellationToken);
        if (alreadyOpen) return;

        var now = DateTime.UtcNow;
        _db.SlaTrackings.Add(new SlaTracking
        {
            Id = Guid.NewGuid(),
            SlaPolicyId = policy.Id,
            EntityId = entityId,
            OpenedAt = now,
            TargetAt = now.AddHours(policy.TargetHours),
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task CloseAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        var policy = await _db.SlaPolicies.FirstOrDefaultAsync(p => p.EntityType == entityType, cancellationToken);
        if (policy is null) return;

        var tracking = await _db.SlaTrackings.FirstOrDefaultAsync(
            t => t.SlaPolicyId == policy.Id && t.EntityId == entityId && t.ResolvedAt == null,
            cancellationToken);
        if (tracking is null) return;

        tracking.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
