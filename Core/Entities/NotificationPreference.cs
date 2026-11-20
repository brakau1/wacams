using System.ComponentModel.DataAnnotations;
using WACAMS.Core.Enums;

namespace WACAMS.Core.Entities;

/// <summary>
/// User notification preferences - controls which channels a user receives notifications on
/// </summary>
public class NotificationPreference
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Employee ID this preference belongs to
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EmployeeId { get; set; } = string.Empty;

    /// <summary>
    /// Event type this preference applies to
    /// </summary>
    [Required]
    public NotificationEventType EventType { get; set; }

    /// <summary>
    /// Channel this preference applies to
    /// </summary>
    [Required]
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Whether this notification type is enabled for this user
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// When the preference was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
