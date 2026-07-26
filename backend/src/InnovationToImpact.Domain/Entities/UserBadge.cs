namespace InnovationToImpact.Domain.Entities;

public class UserBadge
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid BadgeId { get; set; }
    public Badge Badge { get; set; } = null!;

    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}
