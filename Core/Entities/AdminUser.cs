namespace WACAMS.Core.Entities;

/// <summary>
/// Represents a user with administrative privileges beyond their normal role.
/// Allows granting admin access to specific users without changing their job role.
/// </summary>
public class AdminUser
{
    /// <summary>
    /// Surrogate primary key — GUID internally.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Email address of the admin user (unique identifier).
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Display name for audit logs and UI.
    /// </summary>
    [Required]
    [MaxLength(150)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Optional link to Employee record if the admin is also an employee.
    /// </summary>
    [MaxLength(50)]
    public string? EmployeeId { get; set; }

    /// <summary>
    /// Reason for granting admin access (for audit purposes).
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }

    /// <summary>
    /// EmployeeId of the person who granted this admin access.
    /// </summary>
    [MaxLength(50)]
    public string? GrantedBy { get; set; }

    /// <summary>
    /// Timestamp when admin access was granted.
    /// </summary>
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional expiration date for admin access (null = never expires).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether this admin access is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Timestamp when this record was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
