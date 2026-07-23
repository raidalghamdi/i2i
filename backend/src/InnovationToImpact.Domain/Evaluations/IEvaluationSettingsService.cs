namespace InnovationToImpact.Domain.Evaluations;

public sealed record EvaluationSettings(decimal PassThreshold, DateTime? UpdatedAt);

public sealed record PassThresholdUpdateResult(bool Success, decimal Value);

public sealed record EvaluationSettingsPatch(decimal? PassThreshold);

public interface IEvaluationSettingsService
{
    Task<EvaluationSettings> GetAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetPassThresholdAsync(CancellationToken cancellationToken = default);
    Task<PassThresholdUpdateResult> UpdatePassThresholdAsync(decimal value, Guid actorId, CancellationToken cancellationToken = default);
}
