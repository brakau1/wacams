namespace WACAMS.Core.Entities;

/// <summary>
/// Represents a department within the organization.
/// Each department has a supervisor, an attendance target percentage, and a collection of employees.
/// </summary>
public class Department
{
    /// <summary>
    /// Surrogate primary key — GUID internally.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Human-readable department name (e.g., "Production", "Quality").
    /// </summary>
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Short code for the department used in reports and system lookups.
    /// Maps to DepartmentCode enum values.
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Employee ID of the department supervisor responsible for attendance oversight.
    /// Escalation chains start from this person when exceptions are not resolved at lower levels.
    /// </summary>
    [MaxLength(50)]
    public string? SupervisorId { get; set; }

    /// <summary>
    /// Optional description of the department's purpose or function.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Parent department ID for hierarchical organization (null = top-level department).
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Navigation property — parent department in the hierarchy.
    /// </summary>
    public Department? Parent { get; set; }

    /// <summary>
    /// Navigation property — child departments under this department.
    /// </summary>
    public ICollection<Department>? Children { get; set; }

    /// <summary>
    /// Target attendance percentage for this department (e.g., 95.0 = 95%).
    /// </summary>
    public decimal TargetAttendancePercent { get; set; } = 95.0m;

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Record last update timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property — collection of employees assigned to this department.
    /// </summary>
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
