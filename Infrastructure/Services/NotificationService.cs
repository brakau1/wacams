using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WACAMS.Core.DTOs;
using WACAMS.Core.Entities;
using WACAMS.Core.Enums;
using WACAMS.Core.Interfaces;
using WACAMS.Infrastructure.Data;
using WACAMS.Infrastructure.Services;

namespace Infrastructure.Services;

/// <summary>
/// Main notification service that orchestrates template rendering, channel routing, and delivery
/// </summary>
public class NotificationService : INotificationService
{
    private readonly WacamContext _context;
    private readonly TemplateEngine _templateEngine;
    private readonly IEnumerable<INotificationChannel> _channels;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        WacamContext context,
        TemplateEngine templateEngine,
        IEnumerable<INotificationChannel> channels,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _templateEngine = templateEngine;
        _channels = channels;
        _logger = logger;
    }

    /// <summary>
    /// Send a notification event to all configured channels for the recipient
    /// </summary>
    public async Task SendNotificationAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get employee details
            var employee = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == request.RecipientEmployeeId, cancellationToken);

            if (employee == null)
            {
                _logger.LogWarning("Employee {EmployeeId} not found for notification", request.RecipientEmployeeId);
                return;
            }

            // Determine channels: use request.Channels if specified, otherwise check preferences
            var channels = request.Channels;
            if (channels == null || !channels.Any())
            {
                var preferences = await _context.NotificationPreferences
                    .Where(p => p.EmployeeId == request.RecipientEmployeeId && p.EventType == request.EventType && p.IsEnabled)
                    .Select(p => p.Channel)
                    .ToListAsync(cancellationToken);

                channels = preferences.Any()
                    ? preferences
                    : new List<NotificationChannel> { NotificationChannel.Email, NotificationChannel.MicrosoftTeams };
            }

            // Send to each channel
            foreach (var channel in channels)
            {
                await SendToChannelInternalAsync(channel, request.EventType, request.RecipientEmployeeId, employee, request.TemplateParameters, request.RelatedEntityId, request.RelatedEntityType, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification for event {EventType} to {EmployeeId}", request.EventType, request.RecipientEmployeeId);
        }
    }

    /// <summary>
    /// Send notification through a specific channel (interface method)
    /// </summary>
    public async Task<NotificationSendResult> SendToChannelAsync(NotificationChannel channel, NotificationSendRequest request, CancellationToken cancellationToken = default)
    {
        var channelImpl = _channels.FirstOrDefault(c => c.ChannelType == channel);
        if (channelImpl == null)
        {
            _logger.LogWarning("No implementation found for channel {Channel}", channel);
            return new NotificationSendResult { Success = false, ErrorMessage = $"Channel implementation not found for {channel}" };
        }

        return await channelImpl.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Internal method to send notification to a specific channel
    /// </summary>
    private async Task SendToChannelInternalAsync(
        NotificationChannel channel,
        NotificationEventType eventType,
        string recipientEmployeeId,
        Employee employee,
        Dictionary<string, string> templateParameters,
        string? relatedEntityId,
        string? relatedEntityType,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get template for this event type and channel
            var template = await _context.NotificationTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.EventType == eventType && t.Channel == channel && t.IsActive, cancellationToken);

            if (template == null)
            {
                _logger.LogWarning("No active template found for event {EventType} and channel {Channel}", eventType, channel);
                return;
            }

            // Render subject and body
            var subject = _templateEngine.Render(template.SubjectTemplate, templateParameters);
            var body = _templateEngine.Render(template.BodyTemplate, templateParameters);

            // Get recipient address based on channel
            var recipientAddress = GetRecipientAddress(channel, employee);
            if (string.IsNullOrEmpty(recipientAddress))
            {
                _logger.LogWarning("No recipient address for channel {Channel} and employee {EmployeeId}", channel, recipientEmployeeId);
                return;
            }

            // Create notification record
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                EventType = eventType,
                Channel = channel,
                RecipientEmployeeId = recipientEmployeeId,
                RecipientName = employee.DisplayName,
                RecipientAddress = recipientAddress,
                Subject = subject,
                Body = body,
                Priority = GetPriority(eventType),
                Status = NotificationStatus.Pending,
                RelatedEntityId = relatedEntityId,
                RelatedEntityType = relatedEntityType,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(cancellationToken);

            // Get channel implementation
            var channelImpl = _channels.FirstOrDefault(c => c.ChannelType == channel);
            if (channelImpl == null)
            {
                _logger.LogWarning("No implementation found for channel {Channel}", channel);
                notification.Status = NotificationStatus.Failed;
                notification.ErrorMessage = $"Channel implementation not found for {channel}";
                await _context.SaveChangesAsync(cancellationToken);
                return;
            }

            // Send notification
            var request = new NotificationSendRequest
            {
                RecipientEmployeeId = recipientEmployeeId,
                RecipientName = employee.DisplayName,
                RecipientAddress = recipientAddress,
                Subject = subject,
                Body = body,
                Priority = notification.Priority,
                RelatedEntityId = relatedEntityId,
                RelatedEntityType = relatedEntityType,
                TemplateParameters = templateParameters
            };

            var result = await channelImpl.SendAsync(request, cancellationToken);

            // Update notification status
            notification.Status = result.Success ? NotificationStatus.Sent : NotificationStatus.Failed;
            notification.ErrorMessage = result.ErrorMessage;
            notification.SentAt = result.SentAt;
            notification.MessageId = result.MessageId;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Notification {NotificationId} sent via {Channel} to {EmployeeId}: {Status}",
                notification.Id, channel, recipientEmployeeId, notification.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification via {Channel} to {EmployeeId}", channel, recipientEmployeeId);
        }
    }

    /// <summary>
    /// Get recipient address based on channel type
    /// </summary>
    private string? GetRecipientAddress(NotificationChannel channel, Employee employee)
    {
        return channel switch
        {
            NotificationChannel.Email => employee.Email,
            NotificationChannel.MicrosoftTeams => employee.TeamsId,
            NotificationChannel.SMS => employee.Phone,
            NotificationChannel.WhatsApp => employee.Phone,
            _ => null
        };
    }

    /// <summary>
    /// Get priority based on event type
    /// </summary>
    private NotificationPriority GetPriority(NotificationEventType eventType)
    {
        return eventType switch
        {
            NotificationEventType.Escalation => NotificationPriority.Urgent,
            NotificationEventType.ApprovalRequired => NotificationPriority.High,
            NotificationEventType.CorrectiveActionGenerated => NotificationPriority.High,
            NotificationEventType.AttendanceException => NotificationPriority.Normal,
            NotificationEventType.DocumentUploaded => NotificationPriority.Low,
            _ => NotificationPriority.Normal
        };
    }

    /// <summary>
    /// Get notifications for an employee
    /// </summary>
    public async Task<List<NotificationDto>> GetNotificationsForEmployeeAsync(
        string employeeId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var notifications = await _context.Notifications
            .Where(n => n.RecipientEmployeeId == employeeId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                EventType = n.EventType,
                Channel = n.Channel,
                RecipientEmployeeId = n.RecipientEmployeeId,
                RecipientName = n.RecipientName,
                Subject = n.Subject,
                Body = n.Body,
                Priority = n.Priority,
                Status = n.Status,
                RelatedEntityId = n.RelatedEntityId,
                RelatedEntityType = n.RelatedEntityType,
                CreatedAt = n.CreatedAt,
                SentAt = n.SentAt,
                DeliveredAt = n.DeliveredAt,
                ReadAt = n.ReadAt,
                ErrorMessage = n.ErrorMessage
            })
            .ToListAsync(cancellationToken);

        return notifications;
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification != null && notification.ReadAt == null)
        {
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Get notification preferences for an employee
    /// </summary>
    public async Task<List<NotificationPreferenceDto>> GetPreferencesAsync(
        string employeeId,
        CancellationToken cancellationToken = default)
    {
        var preferences = await _context.NotificationPreferences
            .Where(p => p.EmployeeId == employeeId)
            .Select(p => new NotificationPreferenceDto
            {
                EventType = p.EventType,
                Channel = p.Channel,
                IsEnabled = p.IsEnabled
            })
            .ToListAsync(cancellationToken);

        return preferences;
    }

    /// <summary>
    /// Update notification preferences for an employee
    /// </summary>
    public async Task UpdatePreferencesAsync(
        string employeeId,
        UpdateNotificationPreferencesRequest request,
        CancellationToken cancellationToken = default)
    {
        // Remove existing preferences
        var existing = await _context.NotificationPreferences
            .Where(p => p.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);

        _context.NotificationPreferences.RemoveRange(existing);

        // Add new preferences
        var newPreferences = request.Preferences.Select(p => new NotificationPreference
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            EventType = p.EventType,
            Channel = p.Channel,
            IsEnabled = p.IsEnabled,
            UpdatedAt = DateTime.UtcNow
        });

        _context.NotificationPreferences.AddRange(newPreferences);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Get notification statistics
    /// </summary>
    public async Task<NotificationStatsDto> GetStatsAsync(
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications.AsQueryable();

        if (from.HasValue)
            query = query.Where(n => n.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(n => n.CreatedAt <= to.Value);

        var notifications = await query.ToListAsync(cancellationToken);

        var stats = new NotificationStatsDto
        {
            TotalSent = notifications.Count(n => n.Status == NotificationStatus.Sent),
            TotalDelivered = notifications.Count(n => n.Status == NotificationStatus.Delivered),
            TotalFailed = notifications.Count(n => n.Status == NotificationStatus.Failed),
            TotalRead = notifications.Count(n => n.ReadAt != null),
            ByChannel = notifications
                .GroupBy(n => n.Channel)
                .ToDictionary(g => g.Key, g => g.Count()),
            ByEventType = notifications
                .GroupBy(n => n.EventType)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return stats;
    }
}
