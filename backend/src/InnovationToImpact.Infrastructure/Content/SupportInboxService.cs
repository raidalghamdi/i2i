using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.Content;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Content;

public class SupportInboxService : ISupportInboxService
{
    private readonly InnovationDbContext _db;
    private readonly IAuditLogWriter _auditLogWriter;

    public SupportInboxService(InnovationDbContext db, IAuditLogWriter auditLogWriter)
    {
        _db = db;
        _auditLogWriter = auditLogWriter;
    }

    public async Task<SupportInboxPage> ListAsync(SupportInboxFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _db.SupportMessages.AsQueryable();

        if (filter.Handled is not null)
            query = query.Where(m => m.Handled == filter.Handled);

        var total = await query.CountAsync(cancellationToken);
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize is < 1 or > 100 ? 25 : filter.PageSize;

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new SupportInboxPage(items, total, page, pageSize);
    }

    public async Task<SupportInboxCommandResult> MarkHandledAsync(Guid id, Guid? actorId, CancellationToken cancellationToken = default)
    {
        var message = await _db.SupportMessages.SingleOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (message is null) return new SupportInboxCommandResult(SupportInboxCommandStatus.NotFound);

        message.Handled = true;
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("support_message", message.Id, "support_message.handled", actorId, null, cancellationToken);

        return new SupportInboxCommandResult(SupportInboxCommandStatus.Success, message);
    }
}
