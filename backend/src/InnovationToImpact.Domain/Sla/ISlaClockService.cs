namespace InnovationToImpact.Domain.Sla;

public interface ISlaClockService
{
    Task OpenAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);
    Task CloseAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);
}
