namespace InnovationToImpact.Domain.Entities;

public class RoleInvitation
{
    public Guid Id { get; set; }

    public string SamAccountName { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public string? DisplayName { get; set; }
    public string? Email { get; set; }

    public Guid RoleInvitationStatusId { get; set; }
    public RoleInvitationStatus RoleInvitationStatus { get; set; } = null!;

    public DateTime? DeadlineAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public int ReminderCount { get; set; }
    public DateTime? LastReminderAt { get; set; }

    public string Source { get; set; } = "manual";

    public Guid InvitedById { get; set; }
    public User InvitedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
