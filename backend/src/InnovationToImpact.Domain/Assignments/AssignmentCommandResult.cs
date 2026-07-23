using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Assignments;

public sealed record AssignmentCommandResult(AssignmentCommandStatus Status, Assignment? Entity = default);
