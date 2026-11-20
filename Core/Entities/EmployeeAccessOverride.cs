using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WACAMS.Core.Entities;

/// <summary>
/// Represents an access override for an employee, allowing elevated permissions
/// regardless of their job title or standard role assignment.
/// </summary>
public class EmployeeAccessOverride
{
    /// <summary>
    /// Unique identifier for this override record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Employee ID (primary identity key) for whom the override applies.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EmployeeId { get; set; } = string.Empty;

    /// <summary>
    /// The role to override with (e.g., "SuperAdmin", "Admin", "HR").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string OverrideRole { get; set; } = string.Empty;

    /// <summary>
    /// JSON or delimited string representing attendance rule overrides.
    /// Example: "CanEditAttendance,CanApproveExceptions"
    /// </summary>
    [MaxLength(500)]
    public string? AttendanceRuleOverride { get; set; }

    /// <summary>
    /// JSON or delimited string representing escalation rule overrides.
    /// Example: "CanEscalateToHR,CanEscalateToManager"
    /// </summary>
    [MaxLength(500)]
    public string? EscalationOverride { get; set; }

    /// <summary>
    /// JSON or delimited string representing disciplinary rule overrides.
    /// Example: "CanIssueWarnings,CanApproveDisciplinary"
    /// </summary>
    [MaxLength(500)]
    public string? DisciplinaryOverride { get; set; }

    /// <summary>
    /// JSON or delimited string representing dashboard access overrides.
    /// Example: "CanViewAllDepartments,CanViewAuditLogs"
    /// </summary>
    [MaxLength(500)]
    public string? DashboardOverride { get; set; }

    /// <summary>
    /// Date and time when this override becomes effective.
    /// </summary>
    public DateTime EffectiveFrom { get; set; }

    /// <summary>
    /// Date and time when this override expires. Null means no expiration.
    /// </summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>
    /// Optional notes explaining the reason for this override.
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Employee ID of the user who created this override.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string CreatedByEmployeeId { get; set; } = string.Empty;

    /// <summary>
    /// Date and time when this override was created.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date and time when this override was last updated.
    /// </summary>
    public DateTime? UpdatedDate { get; set; }

    /// <summary>
    /// Whether this override is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Employee ID of the user who revoked this override.
    /// </summary>
    [MaxLength(50)]
    public string? RevokedByEmployeeId { get; set; }

    /// <summary>
    /// Date and time when this override was revoked.
    /// </summary>
    public DateTime? RevokedDate { get; set; }

    /// <summary>
    /// Reason for revoking this override.
    /// </summary>
    [MaxLength(1000)]
    public string? RevocationReason { get; set; }

    /// <summary>
    /// Navigation property to the employee.
    /// </summary>
    [ForeignKey("EmployeeId")]
    public Employee? Employee { get; set; }

    /// <summary>
    /// Navigation property to the user who created this override.
    /// </summary>
    [ForeignKey("CreatedByEmployeeId")]
    public Employee? CreatedByEmployee { get; set; }

    /// <summary>
    /// Navigation property to the user who revoked this override.
    /// </summary>
    [ForeignKey("RevokedByEmployeeId")]
    public Employee? RevokedByEmployee { get; set; }
}
