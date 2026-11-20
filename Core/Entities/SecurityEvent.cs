using System.ComponentModel.DataAnnotations;

namespace WACAMS.Core.Entities;

/// <summary>
/// Security event log for tracking security-related activities.
/// Captures authentication failures, privilege escalation attempts, and security policy violations.
/// </summary>
public class SecurityEvent
{
    /// <summary>
    /// Surrogate primary key.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Event type (e.g., "LoginFailed", "PrivilegeEscalationAttempt", "UnauthorizedAccess", "AccountLocked").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Severity level (Low, Medium, High, Critical).
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Severity { get; set; } = "Medium";

    /// <summary>
    /// Employee ID involved in the event (if applicable).
    /// </summary>
    [MaxLength(50)]
    public string? EmployeeId { get; set; }

    /// <summary>
    /// IP address where the event originated.
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string from the request.
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Request path or endpoint.
    /// </summary>
    [MaxLength(500)]
    public string? RequestPath { get; set; }

    /// <summary>
    /// Additional event details in JSON format.
    /// </summary>
    public string? DetailsJson { get; set; }

    /// <summary>
    /// Whether the event has been acknowledged by an administrator.
    /// </summary>
    public bool IsAcknowledged { get; set; } = false;

    /// <summary>
    /// Employee ID who acknowledged the event.
    /// </summary>
    [MaxLength(50)]
    public string? AcknowledgedBy { get; set; }

    /// <summary>
    /// Timestamp when the event was acknowledged.
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// Event occurrence timestamp.
    /// </summary>
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the employee.
    /// </summary>
    public Employee? Employee { get; set; }
}
