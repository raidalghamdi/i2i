using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Screening;

public sealed record ScreeningCommandResult(ScreeningCommandStatus Status, Idea? Idea = null);
