using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Ideas;

public sealed record ChallengeCommandResult(ChallengeCommandStatus Status, Challenge? Entity = default);
