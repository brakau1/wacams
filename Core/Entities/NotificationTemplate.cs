using System.ComponentModel.DataAnnotations;
using WACAMS.Core.Enums;

namespace WACAMS.Core.Entities;

/// <summary>
/// Notification template - reusable template for notification content
/// </summary>
public class NotificationTemplate
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Template name (e.g., "AttendanceException_Email")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Event type this template is for
    /// </summary>
    [Required]
    public NotificationEventType EventType { get; set; }

    /// <summary>
    /// Channel this template is for
    /// </summary>
    [Required]
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Subject template (supports {{placeholder}} syntax)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string SubjectTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Body template (supports {{placeholder}} syntax)
    /// </summary>
    [Required]
    public string BodyTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Whether this template is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the template was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the template was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
