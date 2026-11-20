using WACAMS.Core.Enums;

namespace WACAMS.Core.DTOs;

/// <summary>
/// Request to send a notification
/// </summary>
public class NotificationSendRequest
{
    /// <summary>
    /// Event type that triggered this notification
    /// </summary>
    public NotificationEventType EventType { get; set; }

    /// <summary>
    /// Recipient's employee ID
    /// </summary>
    public string RecipientEmployeeId { get; set; } = string.Empty;

    /// <summary>
    /// Recipient's display name
    /// </summary>
    public string? RecipientName { get; set; }

    /// <summary>
    /// Channel-specific recipient address (email, phone, Teams ID, etc.)
    /// </summary>
    public string RecipientAddress { get; set; } = string.Empty;

    /// <summary>
    /// Notification subject/title
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Notification body content
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Priority level
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// ID of the related entity (exception ID, warning ID, etc.)
    /// </summary>
    public string? RelatedEntityId { get; set; }

    /// <summary>
    /// Type of the related entity
    /// </summary>
    public string? RelatedEntityType { get; set; }

    /// <summary>
    /// Employee ID of the sender (if applicable)
    /// </summary>
    public string? SenderEmployeeId { get; set; }

    /// <summary>
    /// Additional template parameters for dynamic content
    /// </summary>
    public Dictionary<string, string> TemplateParameters { get; set; } = new();
}

/// <summary>
/// Result of sending a notification
/// </summary>
public class NotificationSendResult
{
    /// <summary>
    /// Whether the notification was sent successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if send failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Channel-specific message ID (for tracking/delivery confirmation)
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// When the message was sent
    /// </summary>
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public static NotificationSendResult Successful(string? messageId = null)
    {
        return new NotificationSendResult
        {
            Success = true,
            MessageId = messageId,
            SentAt = DateTime.UtcNow
        };
    }

    public static NotificationSendResult Failed(string errorMessage)
    {
        return new NotificationSendResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            SentAt = DateTime.UtcNow
        };
    }
}
