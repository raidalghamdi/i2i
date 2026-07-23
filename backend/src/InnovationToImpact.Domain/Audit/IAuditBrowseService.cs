namespace InnovationToImpact.Domain.Audit;

public sealed record AuditBrowseFilter(
    string? EntityType,
    string? Action,
    Guid? ActorId,
    DateTime? From,
    DateTime? To,
    int Page,
    int PageSize);

public sealed record AuditRow(
    Guid Id,
    long ChainSeq,
    DateTime OccurredAt,
    string? ActorName,
    string EntityType,
    Guid EntityId,
    string Action,
    bool Verified);

public sealed record AuditPage(
    IReadOnlyList<AuditRow> Items,
    int Total,
    int Page,
    int PageSize);

public interface IAuditBrowseService
{
    Task<AuditPage> BrowseAsync(AuditBrowseFilter filter, CancellationToken cancellationToken = default);
}
