namespace InnovationToImpact.Domain.Cms;

public sealed record CmsCommandResult<T>(CmsCommandStatus Status, T? Entity = default);
