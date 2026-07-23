namespace InnovationToImpact.Domain.Settings;

public sealed record SettingRow(string Key, string ValueJson, DateTime? UpdatedAt);

public sealed record PlatformSettingPatch(string? ValueJson);

public interface IPlatformSettingsService
{
    Task<IReadOnlyList<SettingRow>> ListAsync(CancellationToken cancellationToken = default);
    Task<SettingRow> UpdateAsync(string key, string valueJson, Guid actorId, CancellationToken cancellationToken = default);
}
