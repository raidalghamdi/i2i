namespace InnovationToImpact.Domain.Audit;

public interface IAuditChainVerifier
{
    Task<AuditChainVerificationResult> VerifyAsync(CancellationToken cancellationToken = default);
}
