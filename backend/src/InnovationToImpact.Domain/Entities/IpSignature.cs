namespace InnovationToImpact.Domain.Entities;

public class IpSignature
{
    public Guid Id { get; set; }

    public Guid IdeaId { get; set; }
    public Idea Idea { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string IpTermsVersion { get; set; } = "v1";
    public DateTime SignedAt { get; set; } = DateTime.UtcNow;
}
