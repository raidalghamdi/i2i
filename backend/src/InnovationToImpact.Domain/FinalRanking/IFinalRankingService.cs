namespace InnovationToImpact.Domain.FinalRanking;

public interface IFinalRankingService
{
    Task<FinalRankingResult> PreviewAsync(CancellationToken cancellationToken = default);
    Task<FinalRankingResult> RunAsync(Guid triggeredById, CancellationToken cancellationToken = default);
}
