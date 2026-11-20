namespace WACAMS.Core.Entities;

/// <summary>
/// Junction table for User-Role many-to-many relationship.
/// A user can have multiple roles; a role can be assigned to multiple users.
/// </summary>
public class UserRole
{
    /// <summary>
    /// Surrogate primary key — GUID internally.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the user receiving the role assignment.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Foreign key to the role being assigned.
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Timestamp when this role assignment was created.
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// EmployeeId of the person who assigned this role (for audit trail).
    /// </summary>
    [MaxLength(100)]
    public string? AssignedBy { get; set; }

    /// <summary>
    /// Navigation property — the user this role is assigned to.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Navigation property — the role being assigned.
    /// </summary>
    public Role Role { get; set; } = null!;
}
