using WACAMS.Infrastructure.Services;
using Wacam.Api.Services;
using Microsoft.EntityFrameworkCore;
using Wacam.Api.Data;
using WACAMS.Core.Entities;
using WACAMS.Core.Enums;

namespace Wacam.Api.Jobs;

/// <summary>
/// Hourly job: Monitor attendance exceptions and flag overdue investigations.
/// Exceptions open for >48h are flagged for supervisor review.
/// </summary>
public class AttendanceExceptionMonitorJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AttendanceExceptionMonitorJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public AttendanceExceptionMonitorJob(IServiceProvider serviceProvider, ILogger<AttendanceExceptionMonitorJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AttendanceExceptionMonitorJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunExceptionCheckAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AttendanceExceptionMonitorJob");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunExceptionCheckAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WacamDbContext>();
        var auditService = scope.ServiceProvider.GetRequiredService<AuditService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();

        var now = DateTime.UtcNow;
        var overdueCutoff = now.AddHours(-48);

        // Find open exceptions older than 48h
        var overdueExceptions = await context.AttendanceExceptions
            .Include(e => e.Employee)
                .ThenInclude(e => e!.Department)
            .Include(e => e.InvestigatedByUser)
            .Where(e => e.Status == InvestigationStatus.Open && e.CreatedAt < overdueCutoff)
            .ToListAsync();

        int flaggedCount = 0;

        foreach (var exception in overdueExceptions)
        {
            // Mark as overdue
            exception.IsOverdue = true;
            exception.UpdatedAt = now;

            // Audit log
            await auditService.LogActionAsync(
                "system",
                "ExceptionOverdue",
                "AttendanceException",
                exception.Id.ToString(),
                null,
                $"Exception flagged as overdue (open for >48h)"
            );

            // Notify supervisor if assigned
            if (exception.InvestigatedByUserId != null)
            {
                await notificationService.NotifyExceptionOverdueAsync(
                    exception.Id,
                    exception.EmployeeId,
                    exception.InvestigatedByUserId
                );
            }

            flaggedCount++;
            _logger.LogInformation("Flagged Exception {Id} as overdue (Employee: {EmployeeId})",
                exception.Id, exception.EmployeeId);
        }

        await context.SaveChangesAsync();

        _logger.LogInformation("AttendanceExceptionMonitorJob completed: {Count} exceptions flagged overdue", flaggedCount);
    }
}
