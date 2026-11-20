using System.ComponentModel.DataAnnotations;

namespace WACAMS.Core.Entities;

/// <summary>
/// User session tracking for active sessions and session management.
/// </summary>
public class UserSession
{
    /// <summary>
    /// Surrogate primary key.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Employee ID of the user.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EmployeeId { get; set; } = string.Empty;

    /// <summary>
    /// Session token or identifier.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string SessionToken { get; set; } = string.Empty;

    /// <summary>
    /// IP address of the session.
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string.
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Session start timestamp.
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last activity timestamp.
    /// </summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Session expiration timestamp.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether the session is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Session end timestamp (if ended).
    /// </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// Reason for session end (e.g., "Logout", "Expired", "ForceLogout").
    /// </summary>
    [MaxLength(100)]
    public string? EndReason { get; set; }

    /// <summary>
    /// Navigation property to the employee.
    /// </summary>
    public Employee? Employee { get; set; }
}
