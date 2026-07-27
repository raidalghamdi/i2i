using System.Net;
using System.Net.Mail;
using InnovationToImpact.Domain.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InnovationToImpact.Infrastructure.Email;

/// <summary>
/// Real SMTP delivery via the BCL <see cref="SmtpClient"/> (no external dependency) — registered only
/// in Production. Suited to an internal corporate relay; if your relay requires modern TLS/OAuth that
/// SmtpClient can't do, swap this implementation for a MailKit-based one (the <see cref="IEmailSender"/>
/// contract stays the same). Transient network/timeout failures are allowed to throw: the
/// EmailOutboxProcessor catches them and drives its retry/attempt logic.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpEmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpEmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendAsync(
        string toEmail,
        string subject,
        string bodyHtml,
        string? bodyText,
        CancellationToken cancellationToken = default)
    {
        var recipient = string.IsNullOrWhiteSpace(_options.RedirectAllToEmail)
            ? toEmail
            : _options.RedirectAllToEmail;

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Timeout = _options.TimeoutSeconds * 1000,
            // Empty username => authenticate as the hosting (IIS app-pool) Windows identity against an
            // internal relay. A set username => explicit SMTP credentials.
            Credentials = string.IsNullOrEmpty(_options.Username)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_options.Username, _options.Password),
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = subject,
            Body = bodyHtml,
            IsBodyHtml = true,
        };
        message.To.Add(recipient);

        // Attach a plain-text alternative alongside the HTML when we have one, so non-HTML clients still
        // get readable content.
        if (!string.IsNullOrWhiteSpace(bodyText))
        {
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(bodyText, null, "text/plain"));
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(bodyHtml, null, "text/html"));
        }

        await client.SendMailAsync(message, cancellationToken);

        var messageId = $"smtp-{Guid.NewGuid()}";
        _logger.LogInformation("Sent email via SMTP {Host}:{Port} to {Recipient} (subject {Subject})",
            _options.Host, _options.Port, recipient, subject);
        return new EmailSendResult(true, "smtp", messageId, null);
    }
}
