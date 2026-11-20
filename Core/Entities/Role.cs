namespace WACAMS.Core.Entities;

/// <summary>
/// Represents a user role in the system.
/// </summary>
public class Role
{
    /// <summary>
    /// Surrogate primary key — GUID internally.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Role name (e.g., "Admin", "Supervisor", "Operator").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the role's purpose and permissions.
    /// </summary>
    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether this role is currently active and can be assigned to users.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Record last update timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property — users assigned to this role.
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// Navigation property — permissions granted to this role.
    /// </summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>
/// System-defined role names.
/// Hierarchy (low → high): User → Supervisor → Manager → HR → Admin → SuperAdmin.
/// </summary>
public static class RoleNames
{
    public const string User = "user";
    public const string Supervisor = "supervisor";
    public const string Manager = "manager";
    public const string HR = "hr";
    public const string Admin = "admin";
    public const string SuperAdmin = "superadmin";

    public static readonly string[] All =
    {
        User, Supervisor, Manager, HR, Admin, SuperAdmin
    };

    /// <summary>
    /// Roles that may be assigned through the normal UI flow.
    /// SuperAdmin is excluded — it can only be provisioned via secure seed / environment.
    /// </summary>
    public static readonly string[] Assignable =
    {
        User, Supervisor, Manager, HR, Admin
    };
}
