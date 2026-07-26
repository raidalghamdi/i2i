using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Committee;

public sealed record CommitteeCommandResult(CommitteeCommandStatus Status, CommitteeDecision? Decision = null, Idea? Idea = null);
