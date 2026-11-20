using System.ComponentModel.DataAnnotations;

namespace WACAMS.Core.Entities;

/// <summary>
/// Tracks which employees have been assigned which permission templates.
/// </summary>
public class PermissionTemplateAssignment
{
    /// <summary>
    /// Surrogate primary key.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the permission template.
    /// </summary>
    public Guid TemplateId { get; set; }

    /// <summary>
    /// Employee ID of the assignee.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EmployeeId { get; set; } = string.Empty;

    /// <summary>
    /// Whether the assignment is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Assignment start date.
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Assignment end date (null = permanent).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Employee ID who made the assignment.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string AssignedBy { get; set; } = string.Empty;

    /// <summary>
    /// Reason for the assignment.
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }

    /// <summary>
    /// Navigation property to the template.
    /// </summary>
    public PermissionTemplate? Template { get; set; }

    /// <summary>
    /// Navigation property to the employee.
    /// </summary>
    public Employee? Employee { get; set; }
}
