namespace InnovationToImpact.Domain.Sla;

public sealed record SlaScanOrchestratorResult(int Scanned, int NewlyBreached, int ApproachingBreach, int EscalationsOpened);

public interface ISlaScanOrchestrator
{
    Task<SlaScanOrchestratorResult> ScanAndEscalateAsync(CancellationToken cancellationToken = default);
}
