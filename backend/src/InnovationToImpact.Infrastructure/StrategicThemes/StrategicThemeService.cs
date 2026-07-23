using System.Text.Json;
using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.StrategicThemes;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.StrategicThemes;

public class StrategicThemeService : IStrategicThemeService
{
    private readonly InnovationDbContext _db;
    private readonly IAuditLogWriter _auditLogWriter;

    public StrategicThemeService(InnovationDbContext db, IAuditLogWriter auditLogWriter)
    {
        _db = db;
        _auditLogWriter = auditLogWriter;
    }

    public async Task<IReadOnlyList<StrategicTheme>> ListAsync(CancellationToken cancellationToken = default) =>
        await _db.StrategicThemes.OrderBy(t => t.Priority).ToListAsync(cancellationToken);

    public async Task<StrategicThemeCommandResult> CreateAsync(StrategicThemeInput input, Guid actorId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.NameAr) || string.IsNullOrWhiteSpace(input.NameEn))
            return new StrategicThemeCommandResult(StrategicThemeCommandStatus.InvalidInput);

        var maxPriority = await _db.StrategicThemes.Select(t => (int?)t.Priority).MaxAsync(cancellationToken) ?? 0;
        var theme = new StrategicTheme
        {
            Id = Guid.NewGuid(),
            NameAr = input.NameAr.Trim(),
            NameEn = input.NameEn.Trim(),
            DescriptionAr = string.IsNullOrWhiteSpace(input.DescriptionAr) ? null : input.DescriptionAr.Trim(),
            DescriptionEn = string.IsNullOrWhiteSpace(input.DescriptionEn) ? null : input.DescriptionEn.Trim(),
            Priority = maxPriority + 1,
            OwnerId = actorId,
        };
        _db.StrategicThemes.Add(theme);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("strategic_theme", theme.Id, "create", actorId, JsonSerializer.Serialize(input), cancellationToken);
        return new StrategicThemeCommandResult(StrategicThemeCommandStatus.Success, theme);
    }

    public async Task<StrategicThemeCommandResult> UpdateAsync(Guid id, StrategicThemeInput input, Guid actorId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.NameAr) || string.IsNullOrWhiteSpace(input.NameEn))
            return new StrategicThemeCommandResult(StrategicThemeCommandStatus.InvalidInput);

        var theme = await _db.StrategicThemes.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (theme is null) return new StrategicThemeCommandResult(StrategicThemeCommandStatus.NotFound);

        theme.NameAr = input.NameAr.Trim();
        theme.NameEn = input.NameEn.Trim();
        theme.DescriptionAr = string.IsNullOrWhiteSpace(input.DescriptionAr) ? null : input.DescriptionAr.Trim();
        theme.DescriptionEn = string.IsNullOrWhiteSpace(input.DescriptionEn) ? null : input.DescriptionEn.Trim();
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("strategic_theme", theme.Id, "update", actorId, JsonSerializer.Serialize(input), cancellationToken);
        return new StrategicThemeCommandResult(StrategicThemeCommandStatus.Success, theme);
    }

    public async Task<StrategicThemeCommandResult> DeleteAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default)
    {
        var theme = await _db.StrategicThemes.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (theme is null) return new StrategicThemeCommandResult(StrategicThemeCommandStatus.NotFound);

        var isReferenced = await _db.Ideas.AnyAsync(i => i.StrategicThemeId == id, cancellationToken);
        if (isReferenced) return new StrategicThemeCommandResult(StrategicThemeCommandStatus.InUse);

        _db.StrategicThemes.Remove(theme);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("strategic_theme", theme.Id, "delete", actorId, JsonSerializer.Serialize(new { theme.NameEn }), cancellationToken);
        return new StrategicThemeCommandResult(StrategicThemeCommandStatus.Success, theme);
    }
}
