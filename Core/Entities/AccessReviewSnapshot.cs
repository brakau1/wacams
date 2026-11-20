using System.ComponentModel.DataAnnotations;

namespace WACAMS.Core.Entities;

/// <summary>
/// Snapshot of user access state for rollback and audit purposes.
/// Captures full permission state at a point in time.
/// </summary>
public class AccessReviewSnapshot
{
    public Guid Id { get; set; }

    [Required, MaxLength(50)]
    public string EmployeeId { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? DisplayName { get; set; }

    [MaxLength(50)]
    public string? Department { get; set; }

    [MaxLength(50)]
    public string? Role { get; set; }

    /// <summary>
    /// JSON: assigned permissions { "modules": {...} }
    /// </summary>
    public string AssignedPermissionsJson { get; set; } = "{}";

    /// <summary>
    /// JSON: inherited permissions from role
    /// </summary>
    public string InheritedPermissionsJson { get; set; } = "{}";

    /// <summary>
    /// JSON: active overrides
    /// </summary>
    public string OverridesJson { get; set; } = "[]";

    [MaxLength(45)]
    public string? LastLoginIp { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime? LastRoleChangeAt { get; set; }

    public DateTime? LastPermissionChangeAt { get; set; }

    public DateTime SnapshotAt { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(50)]
    public string CapturedBy { get; set; } = string.Empty;

    public string? Reason { get; set; }

    public Employee? Employee { get; set; }
}
