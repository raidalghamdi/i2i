namespace InnovationToImpact.Infrastructure.Email;

public class EmailOutboxOptions
{
    public int MaxAttempts { get; set; } = 5;
}
