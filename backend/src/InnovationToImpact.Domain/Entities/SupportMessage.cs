namespace InnovationToImpact.Domain.Entities;

public class SupportMessage
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool Handled { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
