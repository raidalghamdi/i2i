using InnovationToImpact.Domain.Escalations;
using InnovationToImpact.Domain.Sla;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Sla;

public class SlaScanOrchestrator : ISlaScanOrchestrator
{
    private readonly ISlaScanner _scanner;
    private readonly IEscalationService _escalationService;
    private readonly InnovationDbContext _db;

    public SlaScanOrchestrator(ISlaScanner scanner, IEscalationService escalationService, InnovationDbContext db)
    {
        _scanner = scanner;
        _escalationService = escalationService;
        _db = db;
    }

    public async Task<SlaScanOrchestratorResult> ScanAndEscalateAsync(CancellationToken cancellationToken = default)
    {
        var result = await _scanner.ScanAsync(cancellationToken);

        var escalationsOpened = 0;
        if (result.NewlyBreachedTrackingIds.Count > 0)
        {
            var breachedRows = await _db.SlaTrackings
                .Include(t => t.SlaPolicy)
                .Where(t => result.NewlyBreachedTrackingIds.Contains(t.Id))
                .ToListAsync(cancellationToken);

            foreach (var row in breachedRows)
            {
                var (labelAr, labelEn) = row.SlaPolicy.EntityType switch
                {
                    "evaluation" => ("التقييم", "evaluation"),
                    "committee" => ("اللجنة", "committee"),
                    _ => (row.SlaPolicy.EntityType, row.SlaPolicy.EntityType),
                };
                var reasonAr = $"تجاوز الحد الزمني المتفق عليه لـ {labelAr} ({row.SlaPolicy.TargetHours} ساعة)";
                var reasonEn = $"SLA breach: {labelEn} exceeded target of {row.SlaPolicy.TargetHours}h";

                await _escalationService.OpenIfAbsentAsync(row.SlaPolicy.EntityType, row.EntityId, reasonAr, reasonEn, cancellationToken);
                escalationsOpened++;
            }
        }

        return new SlaScanOrchestratorResult(result.Scanned, result.NewlyBreached, result.ApproachingBreach, escalationsOpened);
    }
}
