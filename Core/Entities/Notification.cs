using System.ComponentModel.DataAnnotations;
using WACAMS.Core.Enums;

namespace WACAMS.Core.Entities;

/// <summary>
/// Notification entity - represents a single notification sent to a user
/// </summary>
public class Notification
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The event type that triggered this notification
    /// </summary>
    [Required]
    public NotificationEventType EventType { get; set; }

    /// <summary>
    /// The delivery channel used
    /// </summary>
    [Required]
    public NotificationChannel Channel { get; set; }

    /// <summary>
    /// Target user's employee ID
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string RecipientEmployeeId { get; set; } = string.Empty;

    /// <summary>
    /// Recipient's display name (denormalized for quick display)
    /// </summary>
    [MaxLength(200)]
    public string? RecipientName { get; set; }

    /// <summary>
    /// Channel-specific recipient address (email, phone, Teams ID, etc.)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string RecipientAddress { get; set; } = string.Empty;

    /// <summary>
    /// Notification subject/title
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Notification body content (plain text or HTML)
    /// </summary>
    [Required]
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Priority level
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Current delivery status
    /// </summary>
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Error message if delivery failed
    /// </summary>
    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// ID of the related entity (exception ID, warning ID, etc.)
    /// </summary>
    [MaxLength(100)]
    public string? RelatedEntityId { get; set; }

    /// <summary>
    /// Type of the related entity
    /// </summary>
    [MaxLength(100)]
    public string? RelatedEntityType { get; set; }

    /// <summary>
    /// When the notification was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// External message ID from the channel provider (e.g., email Message-ID, SMS SID)
    /// </summary>
    [MaxLength(500)]
    public string? MessageId { get; set; }

    /// <summary>
    /// When the notification was sent
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// When the notification was delivered
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// When the notification was read
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// Employee ID of the sender (if applicable)
    /// </summary>
    [MaxLength(50)]
    public string? SenderEmployeeId { get; set; }

    // Alias property for compatibility
    public string RecipientUser => RecipientName ?? RecipientAddress;
}
