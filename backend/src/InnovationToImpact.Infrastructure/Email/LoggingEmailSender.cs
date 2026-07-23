using InnovationToImpact.Domain.Email;
using Microsoft.Extensions.Logging;

namespace InnovationToImpact.Infrastructure.Email;

public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task<EmailSendResult> SendAsync(
        string toEmail,
        string subject,
        string bodyHtml,
        string? bodyText,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Logging email send (no real provider configured): To={ToEmail}, Subject={Subject}", toEmail, subject);
        return Task.FromResult(new EmailSendResult(true, "logging", $"logging-{Guid.NewGuid()}", null));
    }
}
