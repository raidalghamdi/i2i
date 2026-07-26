using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.StrategicThemes;

public sealed record StrategicThemeCommandResult(StrategicThemeCommandStatus Status, StrategicTheme? Entity = default);
