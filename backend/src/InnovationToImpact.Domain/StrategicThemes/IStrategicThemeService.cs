using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.StrategicThemes;

public interface IStrategicThemeService
{
    Task<IReadOnlyList<StrategicTheme>> ListAsync(CancellationToken cancellationToken = default);
    Task<StrategicThemeCommandResult> CreateAsync(StrategicThemeInput input, Guid actorId, CancellationToken cancellationToken = default);
    Task<StrategicThemeCommandResult> UpdateAsync(Guid id, StrategicThemeInput input, Guid actorId, CancellationToken cancellationToken = default);
    Task<StrategicThemeCommandResult> DeleteAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default);
}
