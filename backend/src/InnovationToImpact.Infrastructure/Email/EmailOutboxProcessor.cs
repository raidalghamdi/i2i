using InnovationToImpact.Domain.Email;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InnovationToImpact.Infrastructure.Email;

public class EmailOutboxProcessor : IEmailOutboxProcessor
{
    private readonly InnovationDbContext _db;
    private readonly IEmailSender _sender;
    private readonly EmailOutboxOptions _options;

    public EmailOutboxProcessor(InnovationDbContext db, IEmailSender sender, IOptions<EmailOutboxOptions> options)
    {
        _db = db;
        _sender = sender;
        _options = options.Value;
    }

    public async Task<EmailOutboxProcessingResult> ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        var pendingStatus = await _db.EmailOutboxStatuses.SingleAsync(s => s.Code == EmailOutboxStatusCodes.Pending, cancellationToken);
        var sentStatus = await _db.EmailOutboxStatuses.SingleAsync(s => s.Code == EmailOutboxStatusCodes.Sent, cancellationToken);
        var failedStatus = await _db.EmailOutboxStatuses.SingleAsync(s => s.Code == EmailOutboxStatusCodes.Failed, cancellationToken);
        var sentLogStatus = await _db.EmailLogStatuses.SingleAsync(s => s.Code == EmailLogStatusCodes.Sent, cancellationToken);
        var failedLogStatus = await _db.EmailLogStatuses.SingleAsync(s => s.Code == EmailLogStatusCodes.Failed, cancellationToken);

        var pendingItems = await _db.EmailOutboxes
            .Where(o => o.EmailOutboxStatusId == pendingStatus.Id)
            .ToListAsync(cancellationToken);

        var sentCount = 0;
        var failedCount = 0;

        foreach (var item in pendingItems)
        {
            item.Attempts++;

            EmailSendResult result;
            try
            {
                result = await _sender.SendAsync(item.ToEmail, item.Subject, item.BodyHtml, item.BodyText, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A real IEmailSender (SMTP/SendGrid/etc.) throws on transient network/timeout
                // errors -- exactly the failure class the retry logic exists to handle. Treat a
                // thrown exception the same as a returned Success:false so Attempts/status still
                // advance correctly, rather than aborting the whole batch and losing this item's
                // Attempts increment. OperationCanceledException propagates instead -- a
                // cancelled call is not a "failed send."
                result = new EmailSendResult(false, "unknown", null, ex.Message);
            }

            if (result.Success)
            {
                item.EmailOutboxStatusId = sentStatus.Id;
                _db.EmailLogs.Add(new EmailLog
                {
                    Id = Guid.NewGuid(),
                    Provider = result.Provider,
                    EmailLogStatusId = sentLogStatus.Id,
                    ProviderMessageId = result.ProviderMessageId,
                    ToEmail = item.ToEmail,
                    ToUserId = item.ToUserId,
                    SentAt = DateTime.UtcNow,
                });
                sentCount++;
            }
            else if (item.Attempts < _options.MaxAttempts)
            {
                item.EmailOutboxStatusId = pendingStatus.Id;
            }
            else
            {
                item.EmailOutboxStatusId = failedStatus.Id;
                _db.EmailLogs.Add(new EmailLog
                {
                    Id = Guid.NewGuid(),
                    Provider = result.Provider,
                    EmailLogStatusId = failedLogStatus.Id,
                    ProviderMessageId = result.ProviderMessageId,
                    ToEmail = item.ToEmail,
                    ToUserId = item.ToUserId,
                    SentAt = DateTime.UtcNow,
                });
                failedCount++;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        return new EmailOutboxProcessingResult(pendingItems.Count, sentCount, failedCount);
    }
}
