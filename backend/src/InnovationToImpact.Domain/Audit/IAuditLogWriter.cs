using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Audit;

public interface IAuditLogWriter
{
    Task<AuditLog> AppendAsync(
        string entityType,
        Guid entityId,
        string action,
        Guid? actorId,
        string? payload,
        CancellationToken cancellationToken = default);
}
