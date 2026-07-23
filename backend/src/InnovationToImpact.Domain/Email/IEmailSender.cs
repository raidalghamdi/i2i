namespace InnovationToImpact.Domain.Email;

public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(
        string toEmail,
        string subject,
        string bodyHtml,
        string? bodyText,
        CancellationToken cancellationToken = default);
}
