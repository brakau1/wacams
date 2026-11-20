namespace WACAMS.Core.Entities;

/// <summary>
/// System-wide configuration settings stored as key-value pairs.
/// Used for runtime-configurable parameters like late thresholds, grace periods, etc.
/// </summary>
public class SystemSettings
{
    /// <summary>
    /// Surrogate primary key — GUID internally.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Unique setting key (e.g., "LateThresholdMinutes", "EarlyLeaveGraceMinutes").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Setting value stored as string — parse to appropriate type in application layer.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of what this setting controls.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Timestamp when this setting was last modified.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// EmployeeId of the user who last modified this setting (for audit trail).
    /// </summary>
    [MaxLength(50)]
    public string? UpdatedBy { get; set; }
}
