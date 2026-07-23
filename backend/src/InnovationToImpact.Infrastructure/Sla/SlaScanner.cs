using InnovationToImpact.Domain.Sla;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Sla;

public class SlaScanner : ISlaScanner
{
    private readonly InnovationDbContext _db;

    public SlaScanner(InnovationDbContext db)
    {
        _db = db;
    }

    public async Task<SlaScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var openTrackings = await _db.SlaTrackings
            .Include(t => t.SlaPolicy)
            .Where(t => t.ResolvedAt == null)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var newlyBreached = 0;
        var approachingBreach = 0;
        var newlyBreachedTrackingIds = new List<Guid>();

        foreach (var tracking in openTrackings)
        {
            if (tracking.BreachedAt is null && now >= tracking.TargetAt)
            {
                tracking.BreachedAt = now;
                newlyBreached++;
                newlyBreachedTrackingIds.Add(tracking.Id);
            }
            else if (tracking.BreachedAt is null)
            {
                var elapsedHours = (now - tracking.OpenedAt).TotalHours;
                var percentElapsed = tracking.SlaPolicy.TargetHours > 0
                    ? (elapsedHours / tracking.SlaPolicy.TargetHours) * 100
                    : 100;

                if (percentElapsed >= tracking.SlaPolicy.WarnAtPct)
                {
                    approachingBreach++;
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new SlaScanResult(openTrackings.Count, newlyBreached, approachingBreach, newlyBreachedTrackingIds);
    }
}
