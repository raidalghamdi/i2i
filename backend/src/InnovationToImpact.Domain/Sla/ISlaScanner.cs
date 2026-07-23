namespace InnovationToImpact.Domain.Sla;

public interface ISlaScanner
{
    Task<SlaScanResult> ScanAsync(CancellationToken cancellationToken = default);
}
