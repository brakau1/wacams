using System.ComponentModel.DataAnnotations;

namespace WACAMS.Core.Entities;

/// <summary>
/// Represents a disciplinary corrective action issued to an employee.
/// Tracks corrective actions with expiry dates and status.
/// </summary>
public class CorrectiveAction
{
    /// <summary>
    /// Surrogate primary key — GUID internally.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Master key referencing the employee receiving the corrective action.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EmployeeId { get; set; } = string.Empty;

    /// <summary>
    /// Type of corrective action (e.g., "Verbal Warning", "Written Warning", "Final Warning").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Detailed reason for the corrective action.
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Employee ID of the manager/supervisor who issued this corrective action.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string IssuedBy { get; set; } = string.Empty;

    /// <summary>
    /// Date the corrective action was issued.
    /// </summary>
    [Required]
    public DateTime DateIssued { get; set; }

    /// <summary>
    /// Date when the corrective action expires and is no longer active.
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Current status of the corrective action (e.g., "Active", "Expired", "Dismissed", "Completed").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Indicates whether the corrective action is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Additional notes or comments about the corrective action.
    /// </summary>
    [MaxLength(4000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Record last update timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property — the employee this corrective action is issued to.
    /// </summary>
    public Employee? Employee { get; set; }
}
