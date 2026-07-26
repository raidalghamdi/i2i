using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Escalations;

public sealed record EscalationCommandResult(EscalationCommandStatus Status, Escalation? Entity = null);
