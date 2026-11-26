using Microsoft.EntityFrameworkCore;
using WACAMS.Core.Enums;
using WACAMS.Api.Services;
using WACAMS.Core.Enums;
using WACAMS.Api.Data;
using WACAMS.Core.Enums;
using WACAMS.Api.Services;
using WACAMS.Core.Enums;

namespace WACAMS.Api.Jobs;

/// <summary>
/// Hourly job: Refresh attendance KPIs and cache for dashboard performance.
/// Recalculates attendance rates, late counts, lost time for all departments.
/// </summary>
public class AttendanceKpiRefreshJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AttendanceKpiRefreshJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public AttendanceKpiRefreshJob(IServiceProvider serviceProvider, ILogger<AttendanceKpiRefreshJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AttendanceKpiRefreshJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshKpisAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AttendanceKpiRefreshJob");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RefreshKpisAsync()
    {
        using var scope = _serviceProvider.CreateScope();
using WACAMS.Core.Enums;
using WACAMS.Api.Services;
using WACAMS.Core.Enums;
        var context = scope.ServiceProvider.GetRequiredService<WacamDbContext>();
        var kpiService = scope.ServiceProvider.GetRequiredService<AttendanceKpiService>();
        var auditService = scope.ServiceProvider.GetRequiredService<AuditService>();

        var today = DateTime.Today;
        var thisMonthStart = new DateTime(today.Year, today.Month, 1);

        // Refresh plant-wide KPIs
        var plantKpis = await kpiService.GetKpisAsync(thisMonthStart, today);
        
        // Refresh department-level KPIs
        var departments = await context.Departments.ToListAsync();
        int deptCount = 0;

        foreach (var dept in departments)
        {
            var deptKpis = await kpiService.GetKpisAsync(thisMonthStart, today, dept.Id);
            deptCount++;
        }

        // Audit log
        await auditService.LogActionAsync(
            "system",
            "KpiRefresh",
            "AttendanceKPI",
            "plant-wide",
            null,
            $"Refreshed KPIs for {deptCount} departments (period: {thisMonthStart:yyyy-MM-dd} to {today:yyyy-MM-dd})"
        );

        _logger.LogInformation("AttendanceKpiRefreshJob completed: Refreshed KPIs for {Count} departments", deptCount);
    }
}
