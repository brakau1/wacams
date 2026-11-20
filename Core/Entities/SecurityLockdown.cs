using System.ComponentModel.DataAnnotations;

namespace WACAMS.Core.Entities;

/// <summary>
/// Security lockdown records for emergency security controls.
/// Tracks system-wide or department-wide security lockdowns.
/// </summary>
public class SecurityLockdown
{
    /// <summary>
    /// Surrogate primary key.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Lockdown type (System, Department, Role).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string LockdownType { get; set; } = string.Empty;

    /// <summary>
    /// Target department ID (if department-level lockdown).
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Target role (if role-level lockdown).
    /// </summary>
    [MaxLength(50)]
    public string? TargetRole { get; set; }

    /// <summary>
    /// Lockdown reason.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Whether the lockdown is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Lockdown start timestamp.
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Lockdown end timestamp (null = active indefinitely).
    /// </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// Employee ID who initiated the lockdown.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string InitiatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Employee ID who ended the lockdown (if applicable).
    /// </summary>
    [MaxLength(50)]
    public string? EndedBy { get; set; }

    /// <summary>
    /// Navigation property to the department.
    /// </summary>
    public Department? Department { get; set; }
}
