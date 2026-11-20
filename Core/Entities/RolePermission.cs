namespace WACAMS.Core.Entities;

/// <summary>
/// Junction table for Role-Permission many-to-many relationship.
/// A role can have multiple permissions; a permission can be assigned to multiple roles.
/// </summary>
public class RolePermission
{
    /// <summary>
    /// Surrogate primary key — GUID internally.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the role receiving the permission.
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Foreign key to the permission being granted.
    /// </summary>
    public Guid PermissionId { get; set; }

    /// <summary>
    /// Timestamp when this permission was assigned to the role.
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// EmployeeId of the person who assigned this permission (for audit trail).
    /// </summary>
    [MaxLength(100)]
    public string? AssignedBy { get; set; }

    /// <summary>
    /// Navigation property — the role this permission is assigned to.
    /// </summary>
    public Role Role { get; set; } = null!;

    /// <summary>
    /// Navigation property — the permission being granted.
    /// </summary>
    public Permission Permission { get; set; } = null!;
}
