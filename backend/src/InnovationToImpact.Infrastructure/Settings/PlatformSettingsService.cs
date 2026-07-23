using System.Text.Json;
using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Settings;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Settings;

public class PlatformSettingsService : IPlatformSettingsService
{
    private readonly InnovationDbContext _db;
    private readonly IAuditLogWriter _auditLogWriter;

    public PlatformSettingsService(InnovationDbContext db, IAuditLogWriter auditLogWriter)
    {
        _db = db;
        _auditLogWriter = auditLogWriter;
    }

    public async Task<IReadOnlyList<SettingRow>> ListAsync(CancellationToken cancellationToken = default) =>
        await _db.AdminSettings
            .OrderBy(s => s.Key)
            .Select(s => new SettingRow(s.Key, s.ValueJson, s.UpdatedAt))
            .ToListAsync(cancellationToken);

    public async Task<SettingRow> UpdateAsync(string key, string valueJson, Guid actorId, CancellationToken cancellationToken = default)
    {
        var row = await _db.AdminSettings.SingleOrDefaultAsync(s => s.Key == key, cancellationToken);
        if (row is null)
        {
            row = new AdminSetting { Key = key };
            _db.AdminSettings.Add(row);
        }

        row.ValueJson = valueJson;
        row.UpdatedById = actorId;
        row.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogWriter.AppendAsync(
            "admin_setting",
            Guid.Empty,
            "admin_setting.updated",
            actorId,
            JsonSerializer.Serialize(new { key, valueJson }),
            cancellationToken);

        return new SettingRow(row.Key, row.ValueJson, row.UpdatedAt);
    }
}
