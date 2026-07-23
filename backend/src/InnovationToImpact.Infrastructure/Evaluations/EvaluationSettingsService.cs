using System.Globalization;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Evaluations;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Evaluations;

public class EvaluationSettingsService : IEvaluationSettingsService
{
    private const string PassThresholdKey = "pass_threshold";

    private readonly InnovationDbContext _db;

    public EvaluationSettingsService(InnovationDbContext db)
    {
        _db = db;
    }

    public async Task<EvaluationSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        var row = await _db.AdminSettings.SingleOrDefaultAsync(s => s.Key == PassThresholdKey, cancellationToken);
        if (row is null) return new EvaluationSettings(EvaluationScoreRules.PassThreshold, null);

        var value = decimal.TryParse(row.ValueJson, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : EvaluationScoreRules.PassThreshold;
        return new EvaluationSettings(value, row.UpdatedAt);
    }

    public async Task<decimal> GetPassThresholdAsync(CancellationToken cancellationToken = default)
        => (await GetAsync(cancellationToken)).PassThreshold;

    public async Task<PassThresholdUpdateResult> UpdatePassThresholdAsync(decimal value, Guid actorId, CancellationToken cancellationToken = default)
    {
        if (value < EvaluationScoreRules.MinScore || value > EvaluationScoreRules.MaxScore)
            return new PassThresholdUpdateResult(false, value);

        var row = await _db.AdminSettings.SingleOrDefaultAsync(s => s.Key == PassThresholdKey, cancellationToken);
        if (row is null)
        {
            row = new AdminSetting { Key = PassThresholdKey };
            _db.AdminSettings.Add(row);
        }
        row.ValueJson = value.ToString(CultureInfo.InvariantCulture);
        row.UpdatedById = actorId;
        row.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new PassThresholdUpdateResult(true, value);
    }
}
