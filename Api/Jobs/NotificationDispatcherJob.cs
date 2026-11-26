using Microsoft.EntityFrameworkCore;
using WACAMS.Api.Data;
using WACAMS.Core.Entities;
using WACAMS.Core.Enums;

namespace WACAMS.Api.Jobs;

/// <summary>
/// Hourly job: Dispatch pending notifications to users.
/// Processes queued notifications and sends via configured channels (email, push, etc.).
/// </summary>
public class NotificationDispatcherJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationDispatcherJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public NotificationDispatcherJob(IServiceProvider serviceProvider, ILogger<NotificationDispatcherJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationDispatcherJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchNotificationsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NotificationDispatcherJob");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task DispatchNotificationsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WacamDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
        var auditService = scope.ServiceProvider.GetRequiredService<AuditService>();

        var now = DateTime.UtcNow;
        int dispatchedCount = 0;
        int failedCount = 0;

        // Find pending notifications
        var pendingNotifications = await context.Notifications
            .Include(n => n.RecipientUser)
            .Where(n => n.Status == NotificationStatus.Pending && n.ScheduledAt <= now)
            .OrderBy(n => n.ScheduledAt)
            .Take(100) // Process in batches
            .ToListAsync();

        foreach (var notification in pendingNotifications)
        {
            try
            {
                // Dispatch based on channel
                switch (notification.Channel)
                {
                    case NotificationChannel.Email:
                        await notificationService.SendEmailNotificationAsync(
                            notification.RecipientUser?.Email ?? "",
                            notification.Subject ?? "",
                            notification.Body ?? ""
                        );
                        break;

                    case NotificationChannel.Push:
                        await notificationService.SendPushNotificationAsync(
                            notification.RecipientUserId,
                            notification.Title ?? "",
                            notification.Body ?? ""
                        );
                        break;

                    case NotificationChannel.SMS:
                        await notificationService.SendSmsNotificationAsync(
                            notification.RecipientUser?.Phone ?? "",
                            notification.Body ?? ""
                        );
                        break;
                }

                // Mark as sent
                notification.Status = NotificationStatus.Sent;
                notification.SentAt = now;
                notification.UpdatedAt = now;
                dispatchedCount++;

                _logger.LogDebug("Dispatched notification {Id} to user {UserId} via {Channel}",
                    notification.Id, notification.RecipientUserId, notification.Channel);
            }
            catch (Exception ex)
            {
                // Mark as failed
                notification.Status = NotificationStatus.Failed;
                notification.ErrorMessage = ex.Message;
                notification.UpdatedAt = now;
                failedCount++;

                _logger.LogWarning(ex, "Failed to dispatch notification {Id}", notification.Id);
            }
        }

        await context.SaveChangesAsync();

        // Audit log
        if (dispatchedCount > 0 || failedCount > 0)
        {
            await auditService.LogActionAsync(
                "system",
                "NotificationDispatch",
                "Notification",
                "batch",
                null,
                $"Dispatched {dispatchedCount} notifications, {failedCount} failed"
            );
        }

        _logger.LogInformation("NotificationDispatcherJob completed: {Dispatched} sent, {Failed} failed",
            dispatchedCount, failedCount);
    }
}
