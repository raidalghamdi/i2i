using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Audit;

public class AuditLogWriter : IAuditLogWriter
{
    private static readonly SemaphoreSlim Lock = new(1, 1);

    private readonly InnovationDbContext _db;

    public AuditLogWriter(InnovationDbContext db)
    {
        _db = db;
    }

    public async Task<AuditLog> AppendAsync(
        string entityType,
        Guid entityId,
        string action,
        Guid? actorId,
        string? payload,
        CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 2;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            await Lock.WaitAsync(cancellationToken);
            try
            {
                var lastEntry = await _db.AuditLogs
                    .OrderByDescending(a => a.ChainSeq)
                    .Select(a => new { a.ChainSeq, a.RowHash })
                    .FirstOrDefaultAsync(cancellationToken);

                var nextSeq = (lastEntry?.ChainSeq ?? 0) + 1;
                var prevHash = lastEntry?.RowHash;
                var occurredAt = DateTime.UtcNow;
                var rowHash = AuditHashComputer.ComputeHash(prevHash, nextSeq, entityType, entityId, action, actorId, payload, occurredAt);

                var entry = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    ChainSeq = nextSeq,
                    RowHash = rowHash,
                    PrevHash = prevHash,
                    EntityType = entityType,
                    EntityId = entityId,
                    Action = action,
                    ActorId = actorId,
                    Payload = payload,
                    OccurredAt = occurredAt,
                };

                _db.AuditLogs.Add(entry);
                await _db.SaveChangesAsync(cancellationToken);
                return entry;
            }
            catch (DbUpdateException) when (attempt < maxAttempts)
            {
                _db.ChangeTracker.Clear();
            }
            finally
            {
                Lock.Release();
            }
        }

        throw new InvalidOperationException("Failed to append audit log entry after retrying due to a ChainSeq conflict.");
    }
}
