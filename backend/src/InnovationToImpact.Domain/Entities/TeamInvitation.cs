namespace InnovationToImpact.Domain.Entities;

public class TeamInvitation
{
    public Guid Id { get; set; }

    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public string InvitedEmail { get; set; } = string.Empty;

    public Guid InvitedById { get; set; }
    public User InvitedBy { get; set; } = null!;

    public string Token { get; set; } = string.Empty;

    public Guid TeamInvitationStatusId { get; set; }
    public TeamInvitationStatus TeamInvitationStatus { get; set; } = null!;

    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(14);
    public DateTime? AcceptedAt { get; set; }
}
