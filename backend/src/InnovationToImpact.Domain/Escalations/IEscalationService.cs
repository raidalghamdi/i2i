using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Escalations;

public interface IEscalationService
{
    Task<Escalation> OpenIfAbsentAsync(string entityType, Guid entityId, string reasonAr, string reasonEn, CancellationToken cancellationToken = default);
    Task<EscalationCommandResult> AcknowledgeAsync(Guid escalationId, Guid actorId, string? notes, CancellationToken cancellationToken = default);
    Task<EscalationCommandResult> BumpAsync(Guid escalationId, Guid actorId, string? notes, CancellationToken cancellationToken = default);
    Task<EscalationCommandResult> ResolveAsync(Guid escalationId, Guid actorId, string resolutionAr, string resolutionEn, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Escalation>> ListAsync(EscalationFilter filter, CancellationToken cancellationToken = default);
    Task<Escalation?> GetAsync(Guid escalationId, CancellationToken cancellationToken = default);
}

public sealed record EscalationActionInput(string? Notes);
public sealed record EscalationResolveInput(string ResolutionAr, string ResolutionEn);
