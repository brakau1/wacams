using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WACAMS.Infrastructure.DTOs.GIS;
using WACAMS.Infrastructure.Services.GIS;

namespace WACAMS.Infrastructure.BackgroundServices;

/// <summary>
/// Background service for scheduled GIS data synchronization
/// Runs at configurable intervals with retry mechanism
/// </summary>
public class GISScheduledSyncService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GISScheduledSyncService> _logger;
    private readonly TimeSpan _syncInterval;
    private readonly int _maxRetryAttempts;
    private readonly TimeSpan _retryDelay;

    public GISScheduledSyncService(
        IServiceProvider serviceProvider,
        ILogger<GISScheduledSyncService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // Configurable sync interval (default: 1 hour)
        var intervalMinutes = configuration.GetValue<int>("GIS:SyncIntervalMinutes", 60);
        _syncInterval = TimeSpan.FromMinutes(intervalMinutes);
        
        // Retry configuration
        _maxRetryAttempts = configuration.GetValue<int>("GIS:MaxRetryAttempts", 3);
        var retryDelaySeconds = configuration.GetValue<int>("GIS:RetryDelaySeconds", 30);
        _retryDelay = TimeSpan.FromSeconds(retryDelaySeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GIS Scheduled Sync Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformScheduledSyncAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scheduled sync cycle");
            }

            // Wait for next sync interval
            await Task.Delay(_syncInterval, stoppingToken);
        }
    }

    private async Task PerformScheduledSyncAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting scheduled sync cycle");

        using var scope = _serviceProvider.CreateScope();
        var gisService = scope.ServiceProvider.GetRequiredService<IGISIntegrationService>();

        var now = DateTime.UtcNow;
        var syncDate = now.Date;

        // Sync employees
        await ExecuteWithRetryAsync(
            () => gisService.SyncAllEmployeesAsync(cancellationToken),
            "EmployeeSync",
            cancellationToken);

        // Sync attendance records for today
        await ExecuteWithRetryAsync(
            () => gisService.SyncAttendanceRecordsAsync(syncDate, syncDate, cancellationToken),
            "AttendanceSync",
            cancellationToken);

        // Sync shift schedules for next 7 days
        await ExecuteWithRetryAsync(
            () => gisService.SyncShiftSchedulesAsync(syncDate, syncDate.AddDays(7), cancellationToken),
            "ShiftScheduleSync",
            cancellationToken);

        // Sync leave records for current month
        var monthStart = new DateTime(syncDate.Year, syncDate.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        await ExecuteWithRetryAsync(
            () => gisService.SyncLeaveRecordsAsync(monthStart, monthEnd, cancellationToken),
            "LeaveRecordSync",
            cancellationToken);

        // Detect exceptions for today
        await ExecuteWithRetryAsync(
            async () =>
            {
                var exceptions = await gisService.DetectExceptionsAsync(syncDate, cancellationToken);
                _logger.LogInformation("Detected {Count} exceptions for today", exceptions.Count);
                return new GISSyncResultDTO { Success = true, RecordsProcessed = exceptions.Count };
            },
            "ExceptionDetection",
            cancellationToken);

        _logger.LogInformation("Completed scheduled sync cycle");
    }

    private async Task ExecuteWithRetryAsync(
        Func<Task<GISSyncResultDTO>> action,
        string operationName,
        CancellationToken cancellationToken)
    {
        int attempt = 0;
        bool success = false;

        while (attempt < _maxRetryAttempts && !success && !cancellationToken.IsCancellationRequested)
        {
            attempt++;
            try
            {
                _logger.LogInformation("Executing {Operation} (Attempt {Attempt}/{MaxAttempts})", 
                    operationName, attempt, _maxRetryAttempts);

                var result = await action();

                if (result.Success)
                {
                    success = true;
                    _logger.LogInformation("{Operation} completed successfully. Processed {Count} records", 
                        operationName, result.RecordsProcessed);
                }
                else
                {
                    _logger.LogWarning("{Operation} failed: {Errors}", 
                        operationName, string.Join(", ", result.Errors));
                    
                    if (attempt < _maxRetryAttempts)
                    {
                        _logger.LogInformation("Retrying {Operation} in {Delay} seconds", 
                            operationName, _retryDelay.TotalSeconds);
                        await Task.Delay(_retryDelay, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during {Operation} (Attempt {Attempt}/{MaxAttempts})", 
                    operationName, attempt, _maxRetryAttempts);

                if (attempt < _maxRetryAttempts)
                {
                    _logger.LogInformation("Retrying {Operation} in {Delay} seconds", 
                        operationName, _retryDelay.TotalSeconds);
                    await Task.Delay(_retryDelay, cancellationToken);
                }
            }
        }

        if (!success)
        {
            _logger.LogError("{Operation} failed after {Attempts} attempts", operationName, _maxRetryAttempts);
        }
    }
}
