using WACAMS.Core.DTOs;
using WACAMS.Core.Enums;

namespace WACAMS.Core.Interfaces;

/// <summary>
/// Service interface for notification operations
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Send a notification event to all configured channels for the recipient
    /// </summary>
    Task SendNotificationAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send notification through a specific channel
    /// </summary>
    Task<NotificationSendResult> SendToChannelAsync(NotificationChannel channel, NotificationSendRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notifications for an employee
    /// </summary>
    Task<List<NotificationDto>> GetNotificationsForEmployeeAsync(string employeeId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark notification as read
    /// </summary>
    Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notification preferences for an employee
    /// </summary>
    Task<List<NotificationPreferenceDto>> GetPreferencesAsync(string employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update notification preferences for an employee
    /// </summary>
    Task UpdatePreferencesAsync(string employeeId, UpdateNotificationPreferencesRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get notification statistics
    /// </summary>
    Task<NotificationStatsDto> GetStatsAsync(DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
}
