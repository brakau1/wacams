using WACAMS.Core.Enums;

namespace WACAMS.Core.DTOs;

/// <summary>
/// DTO for notification list/response
/// </summary>
public class NotificationDto
{
    public Guid Id { get; set; }
    public NotificationEventType EventType { get; set; }
    public NotificationChannel Channel { get; set; }
    public string RecipientEmployeeId { get; set; } = string.Empty;
    public string? RecipientName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationPriority Priority { get; set; }
    public NotificationStatus Status { get; set; }
    public string? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Request to create a notification
/// </summary>
public class CreateNotificationRequest
{
    public NotificationEventType EventType { get; set; }
    public string RecipientEmployeeId { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public List<NotificationChannel> Channels { get; set; } = new();
    public string? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public Dictionary<string, string> TemplateParameters { get; set; } = new();
}

/// <summary>
/// Request to update notification preferences
/// </summary>
public class UpdateNotificationPreferencesRequest
{
    public List<NotificationPreferenceDto> Preferences { get; set; } = new();
}

/// <summary>
/// DTO for notification preferences
/// </summary>
public class NotificationPreferenceDto
{
    public NotificationEventType EventType { get; set; }
    public NotificationChannel Channel { get; set; }
    public bool IsEnabled { get; set; }
}

/// <summary>
/// Notification statistics
/// </summary>
public class NotificationStatsDto
{
    public int TotalSent { get; set; }
    public int TotalDelivered { get; set; }
    public int TotalFailed { get; set; }
    public int TotalRead { get; set; }
    public Dictionary<NotificationChannel, int> ByChannel { get; set; } = new();
    public Dictionary<NotificationEventType, int> ByEventType { get; set; } = new();
}
