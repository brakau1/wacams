namespace WACAMS.Core.Entities;

/// <summary>
/// Immutable audit trail for every Create, Update, and Delete operation.
/// Captures the entity type, business key, action performed, old and new values,
/// who performed the action, when, and from which IP address.
/// All changes to Employees, AttendanceRecords, Exceptions, WarningLetters must generate an AuditLog entry.
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Surrogate primary key — GUID internally.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The domain entity type that was affected (e.g., "Employee", "AttendanceRecord").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// The business key of the affected record (e.g., EmployeeId for Employee entities).
    /// For records without a business key, this stores the surrogate Guid as a string.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// The action performed on the entity.
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialized representation of the entity state BEFORE the change.
    /// Null for Create operations.
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// JSON-serialized representation of the entity state AFTER the change.
    /// Null for Delete operations.
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// Employee ID of the user who performed the action.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string PerformedBy { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the action was performed.
    /// </summary>
    [Required]
    public DateTime PerformedAt { get; set; }

    /// <summary>
    /// IP address from which the action was initiated.
    /// Useful for tracing unauthorized or suspicious activity.
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// HTTP method and endpoint path that triggered this audit entry.
    /// </summary>
    [MaxLength(200)]
    public string? HttpEndpoint { get; set; }

    /// <summary>
    /// JSON summary of what changed (old values → new values).
    /// Alternative structured representation alongside OldValue/NewValue.
    /// </summary>
    public string? ChangesJson { get; set; }

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
