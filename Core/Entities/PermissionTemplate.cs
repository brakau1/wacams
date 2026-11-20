using System.ComponentModel.DataAnnotations;

namespace WACAMS.Core.Entities;

/// <summary>
/// Permission template for bulk permission assignment.
/// Templates define standard permission sets for roles or departments.
/// </summary>
public class PermissionTemplate
{
    /// <summary>
    /// Surrogate primary key.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Template name (e.g., "Operator", "Supervisor", "Manager", "HR", "Admin").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Template description.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Template version number.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Whether the template is active and can be assigned.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Target role for this template (optional).
    /// </summary>
    [MaxLength(50)]
    public string? TargetRole { get; set; }

    /// <summary>
    /// Target department for this template (optional).
    /// </summary>
    public Guid? TargetDepartmentId { get; set; }

    /// <summary>
    /// JSON string containing permission definitions.
    /// Format: { "modules": { "Attendance": { "View": true, "Create": false, ... } } }
    /// </summary>
    [Required]
    public string PermissionsJson { get; set; } = "{}";

    /// <summary>
    /// Employee ID of the creator.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Employee ID of the last modifier.
    /// </summary>
    [MaxLength(50)]
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Record last update timestamp.
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// Navigation property to target department.
    /// </summary>
    public Department? TargetDepartment { get; set; }

    /// <summary>
    /// Navigation property to template assignments.
    /// </summary>
    public ICollection<PermissionTemplateAssignment> Assignments { get; set; } = new List<PermissionTemplateAssignment>();

    /// <summary>
    /// Navigation property to template version history.
    /// </summary>
    public ICollection<PermissionTemplateVersion> Versions { get; set; } = new List<PermissionTemplateVersion>();
}
