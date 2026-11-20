namespace WACAMS.Core.Enums;

/// <summary>
/// Types of events that trigger notifications
/// </summary>
public enum NotificationEventType
{
    AttendanceException = 1,
    CorrectiveActionGenerated = 2,
    ApprovalRequired = 3,
    Escalation = 4,
    DocumentUploaded = 5
}
