using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Sla;

// Change 20260726
public sealed record SlaPolicyInput(
    string EntityType,
    string FromState,
    string ToState,
    int TargetHours,
    int WarnAtPct);

public enum SlaPolicyCommandStatus
{
    Success,
    NotFound,
    DuplicateTransition,
    Invalid,
}

public sealed record SlaPolicyCommandResult(SlaPolicyCommandStatus Status, SlaPolicy? Entity);

public interface ISlaPolicyService
{
    Task<IReadOnlyList<SlaPolicy>> ListAllAsync(CancellationToken cancellationToken = default);
    Task<SlaPolicy?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SlaPolicyCommandResult> CreateAsync(SlaPolicyInput input, Guid actorId, CancellationToken cancellationToken = default);
    Task<SlaPolicyCommandResult> UpdateAsync(Guid id, SlaPolicyInput input, Guid actorId, CancellationToken cancellationToken = default);
    Task<SlaPolicyCommandResult> DeleteAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default);
}
