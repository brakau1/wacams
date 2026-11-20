using System.ComponentModel.DataAnnotations;

namespace WACAMS.Core.Entities;

/// <summary>
/// Tracks bulk import operations for audit and rollback.
/// </summary>
public class BulkImportHistory
{
    public Guid Id { get; set; }

    /// <summary>
    /// Import type (UserCreation, RoleAssignment, RoleRemoval, DepartmentAssignment, OverrideCreation, AccountActivation, AccountDeactivation).
    /// </summary>
    [Required, MaxLength(100)]
    public string ImportType { get; set; } = string.Empty;

    /// <summary>
    /// Source file name.
    /// </summary>
    [Required, MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Total rows in the import file.
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// Successfully processed rows.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Failed rows.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// JSON array of error details: [{ row, field, message }]
    /// </summary>
    public string? ErrorDetailsJson { get; set; }

    /// <summary>
    /// Status (Pending, Processing, Completed, Failed, RolledBack).
    /// </summary>
    [Required, MaxLength(20)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Employee ID who initiated the import.
    /// </summary>
    [Required, MaxLength(50)]
    public string InitiatedBy { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Whether this import has been rolled back.
    /// </summary>
    public bool IsRolledBack { get; set; } = false;

    /// <summary>
    /// Employee ID who performed rollback (if any).
    /// </summary>
    [MaxLength(50)]
    public string? RolledBackBy { get; set; }

    public DateTime? RolledBackAt { get; set; }
}
