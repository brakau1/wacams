using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WACAMS.Core.DTOs;
using WACAMS.Core.Enums;
using WACAMS.Core.Interfaces;

namespace Wacam.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Get notifications for the current user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> GetMyNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var employeeId = User.FindFirst("employeeId")?.Value;
        if (string.IsNullOrEmpty(employeeId))
        {
            return Unauthorized();
        }

        var notifications = await _notificationService.GetNotificationsForEmployeeAsync(
            employeeId, page, pageSize, cancellationToken);

        return Ok(notifications);
    }

    /// <summary>
    /// Get unread notification count for the current user
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount(CancellationToken cancellationToken = default)
    {
        var employeeId = User.FindFirst("employeeId")?.Value;
        if (string.IsNullOrEmpty(employeeId))
        {
            return Unauthorized();
        }

        var notifications = await _notificationService.GetNotificationsForEmployeeAsync(
            employeeId, 1, 1000, cancellationToken);

        var unreadCount = notifications.Count(n => n.ReadAt == null);
        return Ok(unreadCount);
    }

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    [HttpPost("{notificationId}/read")]
    public async Task<IActionResult> MarkAsRead(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var employeeId = User.FindFirst("employeeId")?.Value;
        if (string.IsNullOrEmpty(employeeId))
        {
            return Unauthorized();
        }

        await _notificationService.MarkAsReadAsync(notificationId, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Get notification preferences for the current user
    /// </summary>
    [HttpGet("preferences")]
    public async Task<ActionResult<List<NotificationPreferenceDto>>> GetPreferences(CancellationToken cancellationToken = default)
    {
        var employeeId = User.FindFirst("employeeId")?.Value;
        if (string.IsNullOrEmpty(employeeId))
        {
            return Unauthorized();
        }

        var preferences = await _notificationService.GetPreferencesAsync(employeeId, cancellationToken);
        return Ok(preferences);
    }

    /// <summary>
    /// Update notification preferences for the current user
    /// </summary>
    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] List<NotificationPreferenceDto> preferences,
        CancellationToken cancellationToken = default)
    {
        var employeeId = User.FindFirst("employeeId")?.Value;
        if (string.IsNullOrEmpty(employeeId))
        {
            return Unauthorized();
        }

        await _notificationService.UpdatePreferencesAsync(employeeId, preferences, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Get notification statistics (admin only)
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<NotificationStatsDto>> GetStats(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var stats = await _notificationService.GetStatsAsync(from, to, cancellationToken);
        return Ok(stats);
    }

    /// <summary>
    /// Send a test notification (for debugging)
    /// </summary>
    [HttpPost("test")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> SendTestNotification(
        [FromQuery] NotificationChannel channel = NotificationChannel.Email,
        CancellationToken cancellationToken = default)
    {
        var employeeId = User.FindFirst("employeeId")?.Value;
        if (string.IsNullOrEmpty(employeeId))
        {
            return Unauthorized();
        }

        var templateParameters = new Dictionary<string, string>
        {
            ["EmployeeName"] = "Test User",
            ["EmployeeId"] = employeeId,
            ["DepartmentName"] = "Test Department",
            ["ExceptionType"] = "Test Exception",
            ["Date"] = DateTime.Now.ToString("yyyy-MM-dd"),
            ["LostMinutes"] = "30",
            ["AppUrl"] = "http://localhost:5173"
        };

        await _notificationService.SendNotificationAsync(
            NotificationEventType.AttendanceException,
            employeeId,
            templateParameters,
            "test-id",
            "TestEntity",
            cancellationToken);

        return Ok(new { message = "Test notification sent" });
    }
}
