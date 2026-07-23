namespace InnovationToImpact.Domain.Email;

public static class EmailOutboxStatusCodes
{
    public const string Pending = "pending";
    public const string Sending = "sending";
    public const string Sent = "sent";
    public const string Failed = "failed";
    public const string Skipped = "skipped";
}
