using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Phases;

public sealed record PhaseScheduleCommandResult(PhaseScheduleCommandStatus Status, PhaseSchedule? Entity = default);
