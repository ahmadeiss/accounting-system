namespace Accounting.Core.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    // FK: Role (required)
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;

    // FK: Branch (null = access to all branches)
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }

    /// <summary>Navigation to all issued refresh tokens (use RefreshToken table, not this collection directly).</summary>
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public string FullName => $"{FirstName} {LastName}".Trim();
}

