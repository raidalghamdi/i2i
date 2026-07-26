namespace InnovationToImpact.Domain.Email;

public interface IEmailOutboxProcessor
{
    Task<EmailOutboxProcessingResult> ProcessPendingAsync(CancellationToken cancellationToken = default);
}
