using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Audit;

public class AuditBrowseService : IAuditBrowseService
{
    private readonly InnovationDbContext _db;
    private readonly IAuditChainVerifier _verifier;

    public AuditBrowseService(InnovationDbContext db, IAuditChainVerifier verifier)
    {
        _db = db;
        _verifier = verifier;
    }

    public async Task<AuditPage> BrowseAsync(AuditBrowseFilter filter, CancellationToken ct = default)
    {
        var query = _db.AuditLogs.Include(a => a.Actor).AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.EntityType)) query = query.Where(a => a.EntityType == filter.EntityType);
        if (!string.IsNullOrWhiteSpace(filter.Action)) query = query.Where(a => a.Action.Contains(filter.Action));
        if (filter.ActorId is not null) query = query.Where(a => a.ActorId == filter.ActorId);
        if (filter.From is not null) query = query.Where(a => a.OccurredAt >= filter.From);
        if (filter.To is not null) query = query.Where(a => a.OccurredAt <= filter.To);

        var total = await query.CountAsync(ct);
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize is < 1 or > 100 ? 25 : filter.PageSize;
        var rows = await query.OrderByDescending(a => a.ChainSeq)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new { a.Id, a.ChainSeq, a.OccurredAt, ActorName = a.Actor != null ? a.Actor.FullNameEn : null, a.EntityType, a.EntityId, a.Action })
            .ToListAsync(ct);

        var verification = await _verifier.VerifyAsync(ct);
        var items = rows.Select(r => new AuditRow(r.Id, r.ChainSeq, r.OccurredAt, r.ActorName, r.EntityType, r.EntityId, r.Action,
            verification.IsValid || (verification.BrokenAtChainSeq is long brk && r.ChainSeq < brk))).ToList();
        return new AuditPage(items, total, page, pageSize);
    }
}
