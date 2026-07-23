using System.Text.Json;
using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Ideas;

public class ChallengeService : IChallengeService
{
    private readonly InnovationDbContext _db;
    private readonly IAuditLogWriter _auditLogWriter;

    public ChallengeService(InnovationDbContext db, IAuditLogWriter auditLogWriter)
    {
        _db = db;
        _auditLogWriter = auditLogWriter;
    }

    public async Task<IReadOnlyList<Challenge>> ListAsync(CancellationToken cancellationToken = default) =>
        await _db.Challenges.OrderBy(c => c.StrategicThemeId).ThenBy(c => c.SortOrder).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Challenge>> ListActiveByThemeAsync(Guid themeId, CancellationToken cancellationToken = default) =>
        await _db.Challenges.Where(c => c.StrategicThemeId == themeId && c.IsActive).OrderBy(c => c.SortOrder).ToListAsync(cancellationToken);

    public async Task<Challenge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.Challenges.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<ChallengeCommandResult> CreateAsync(ChallengeInput input, Guid actorId, CancellationToken cancellationToken = default)
    {
        var themeExists = await _db.StrategicThemes.AnyAsync(t => t.Id == input.StrategicThemeId, cancellationToken);
        if (!themeExists) return new ChallengeCommandResult(ChallengeCommandStatus.InvalidStrategicTheme);

        var challenge = new Challenge
        {
            Id = Guid.NewGuid(),
            StrategicThemeId = input.StrategicThemeId,
            TextAr = input.TextAr,
            TextEn = input.TextEn,
            SortOrder = input.SortOrder,
            IsActive = input.IsActive,
        };
        _db.Challenges.Add(challenge);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("challenge", challenge.Id, "create", actorId, JsonSerializer.Serialize(input), cancellationToken);
        return new ChallengeCommandResult(ChallengeCommandStatus.Success, challenge);
    }

    public async Task<ChallengeCommandResult> UpdateAsync(Guid id, ChallengeInput input, Guid actorId, CancellationToken cancellationToken = default)
    {
        var challenge = await _db.Challenges.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (challenge is null) return new ChallengeCommandResult(ChallengeCommandStatus.NotFound);

        var themeExists = await _db.StrategicThemes.AnyAsync(t => t.Id == input.StrategicThemeId, cancellationToken);
        if (!themeExists) return new ChallengeCommandResult(ChallengeCommandStatus.InvalidStrategicTheme);

        challenge.StrategicThemeId = input.StrategicThemeId;
        challenge.TextAr = input.TextAr;
        challenge.TextEn = input.TextEn;
        challenge.SortOrder = input.SortOrder;
        challenge.IsActive = input.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("challenge", challenge.Id, "update", actorId, JsonSerializer.Serialize(input), cancellationToken);
        return new ChallengeCommandResult(ChallengeCommandStatus.Success, challenge);
    }

    public async Task<ChallengeCommandResult> DeleteAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default)
    {
        var challenge = await _db.Challenges.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (challenge is null) return new ChallengeCommandResult(ChallengeCommandStatus.NotFound);

        var isReferenced = await _db.Ideas.AnyAsync(i => i.ChallengeId == id, cancellationToken);
        if (isReferenced) return new ChallengeCommandResult(ChallengeCommandStatus.InUse);

        _db.Challenges.Remove(challenge);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("challenge", challenge.Id, "delete", actorId, JsonSerializer.Serialize(new { challenge.TextEn }), cancellationToken);
        return new ChallengeCommandResult(ChallengeCommandStatus.Success, challenge);
    }
}
