namespace InnovationToImpact.Domain.Entities;

public class PendingRoleGrant
{
    public Guid Id { get; set; }
    public string SamAccountName { get; set; } = string.Empty;

    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public Guid GrantedById { get; set; }
    public User GrantedBy { get; set; } = null!;

    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
}
