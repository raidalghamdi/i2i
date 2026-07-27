namespace InnovationToImpact.Infrastructure.Email;

/// <summary>
/// SMTP delivery settings, bound from the "Smtp" configuration section. Only consumed in Production
/// (see Program.cs) where <see cref="SmtpEmailSender"/> is registered. All values are placeholders in
/// appsettings.Production.json and MUST be filled in for the environment — see backend/DEPLOYMENT.md.
/// </summary>
public class SmtpEmailOptions
{
    /// <summary>SMTP server hostname or IP (e.g. an internal Exchange/relay host). Required.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>SMTP port. 587 for STARTTLS submission, 25 for an internal unauthenticated relay.</summary>
    public int Port { get; set; } = 587;

    /// <summary>Enable TLS. Keep true for 587; internal relays on port 25 may use false.</summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// SMTP username. Leave EMPTY to authenticate as the IIS app-pool (Windows) identity against an
    /// internal relay — the common corporate setup. Set it only if the relay requires a distinct login.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>SMTP password. Only used when <see cref="Username"/> is set.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>The From address on outgoing mail. Required.</summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>The From display name on outgoing mail.</summary>
    public string FromName { get; set; } = "Innovation to Impact";

    /// <summary>Per-send timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// OPTIONAL safety valve: if set, ALL outgoing mail is redirected to this single address instead of
    /// the real recipient (useful when validating a fresh production box against live data). Leave EMPTY
    /// for normal delivery — this is off by default so production is never silently intercepting mail.
    /// </summary>
    public string RedirectAllToEmail { get; set; } = string.Empty;
}
