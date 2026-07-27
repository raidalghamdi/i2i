namespace InnovationToImpact.Domain.Reports;

// Change 20260726
public sealed record EvaluatorProductivityRow(
    Guid UserId,
    string DisplayName,
    int AssignedCount,
    int CompletedCount,
    int DraftCount,
    decimal? AvgScore,
    double? AvgTurnaroundHours,
    int CoiCount);

public interface IEvaluatorProductivityService
{
    Task<IReadOnlyList<EvaluatorProductivityRow>> GetAsync(CancellationToken cancellationToken = default);
}
