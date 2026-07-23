namespace InnovationToImpact.Domain.Assignments;

public sealed record BulkUnassignRequest(IReadOnlyList<Guid> Ids);
