namespace InnovationToImpact.Domain.Audit;

public sealed record AuditChainVerificationResult(bool IsValid, long? BrokenAtChainSeq);
