using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Ideas;

public interface IChallengeService
{
    Task<IReadOnlyList<Challenge>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Challenge>> ListActiveByThemeAsync(Guid themeId, CancellationToken cancellationToken = default);
    Task<Challenge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ChallengeCommandResult> CreateAsync(ChallengeInput input, Guid actorId, CancellationToken cancellationToken = default);
    Task<ChallengeCommandResult> UpdateAsync(Guid id, ChallengeInput input, Guid actorId, CancellationToken cancellationToken = default);
    Task<ChallengeCommandResult> DeleteAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default);
}
