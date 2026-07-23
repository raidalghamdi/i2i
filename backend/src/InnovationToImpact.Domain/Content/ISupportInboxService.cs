using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Content;

public sealed record SupportInboxFilter(bool? Handled, int Page, int PageSize);

public sealed record SupportInboxPage(IReadOnlyList<SupportMessage> Items, int Total, int Page, int PageSize);

public enum SupportInboxCommandStatus
{
    Success,
    NotFound,
}

public sealed record SupportInboxCommandResult(SupportInboxCommandStatus Status, SupportMessage? Entity = default);

public interface ISupportInboxService
{
    Task<SupportInboxPage> ListAsync(SupportInboxFilter filter, CancellationToken cancellationToken = default);

    Task<SupportInboxCommandResult> MarkHandledAsync(Guid id, Guid? actorId, CancellationToken cancellationToken = default);
}
