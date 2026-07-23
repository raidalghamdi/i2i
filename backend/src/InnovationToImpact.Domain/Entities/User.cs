namespace InnovationToImpact.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string SamAccountName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullNameAr { get; set; } = string.Empty;
    public string FullNameEn { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? ManagerEmail { get; set; }
    public string? Title { get; set; }
    /// <summary>BCrypt hash for JWT/password login. Null for AD-only users (Negotiate/DevAuth never set this).</summary>
    public string? PasswordHash { get; set; }
    public int Points { get; set; }
    public int Level { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public Guid? EscalationTierId { get; set; }
    public EscalationTier? EscalationTier { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
