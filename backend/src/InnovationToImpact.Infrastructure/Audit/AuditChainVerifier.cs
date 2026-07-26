using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Audit;

public class AuditChainVerifier : IAuditChainVerifier
{
    private readonly InnovationDbContext _db;

    public AuditChainVerifier(InnovationDbContext db)
    {
        _db = db;
    }

    public async Task<AuditChainVerificationResult> VerifyAsync(CancellationToken cancellationToken = default)
    {
        var entries = await _db.AuditLogs
            .OrderBy(a => a.ChainSeq)
            .ToListAsync(cancellationToken);

        string? expectedPrevHash = null;
        foreach (var entry in entries)
        {
            // Compare the stored PrevHash column against the actual previous row's
            // RowHash before trusting anything else about this row. The RowHash
            // recomputation below never reads entry.PrevHash (it always chains from
            // the verifier's own running expectedPrevHash), so a row whose PrevHash
            // column was overwritten in isolation would otherwise still recompute to
            // a matching RowHash and slip through undetected. This check is what
            // actually catches tampering of the PrevHash field itself.
            if (!string.Equals(expectedPrevHash, entry.PrevHash, StringComparison.Ordinal))
            {
                return new AuditChainVerificationResult(IsValid: false, BrokenAtChainSeq: entry.ChainSeq);
            }

            var expectedHash = AuditHashComputer.ComputeHash(
                expectedPrevHash,
                entry.ChainSeq,
                entry.EntityType,
                entry.EntityId,
                entry.Action,
                entry.ActorId,
                entry.Payload,
                entry.OccurredAt);

            if (!string.Equals(expectedHash, entry.RowHash, StringComparison.Ordinal))
            {
                return new AuditChainVerificationResult(IsValid: false, BrokenAtChainSeq: entry.ChainSeq);
            }

            expectedPrevHash = entry.RowHash;
        }

        return new AuditChainVerificationResult(IsValid: true, BrokenAtChainSeq: null);
    }
}
