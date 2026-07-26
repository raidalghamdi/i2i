namespace InnovationToImpact.Domain.Entities;

public class TeamMember
{
    public Guid Id { get; set; }

    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid TeamMemberRoleId { get; set; }
    public TeamMemberRole TeamMemberRole { get; set; } = null!;
}
