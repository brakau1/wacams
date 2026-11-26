using WACAMS.Api.Services;
using Microsoft.EntityFrameworkCore;
using Wacam.Api.Data;

namespace Wacam.Api.Jobs;

/// <summary>
/// Hourly job: Refresh executive-level metrics and strategic KPIs.
/// Updates executive dashboard data, cross-departmental analytics, and strategic insights.
/// </summary>
public class ExecutiveMetricsRefreshJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExecutiveMetricsRefreshJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public ExecutiveMetricsRefreshJob(IServiceProvider serviceProvider, ILogger<ExecutiveMetricsRefreshJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExecutiveMetricsRefreshJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshExecutiveMetricsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExecutiveMetricsRefreshJob");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RefreshExecutiveMetricsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WacamDbContext>();
        var executiveKpiService = scope.ServiceProvider.GetRequiredService<ExecutiveKpiService>();
        var auditService = scope.ServiceProvider.GetRequiredService<AuditService>();

        var today = DateTime.Today;
        var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
        var thisMonthStart = new DateTime(today.Year, today.Month, 1);

        // Refresh executive KPIs for multiple time periods
        var todayKpis = await executiveKpiService.GetExecutiveKpisAsync(today, today);
        var wtdKpis = await executiveKpiService.GetExecutiveKpisAsync(thisWeekStart, today);
        var mtdKpis = await executiveKpiService.GetExecutiveKpisAsync(thisMonthStart, today);

        // Refresh department rankings
        var deptRanking = await executiveKpiService.GetDepartmentRankingAsync(thisMonthStart, today);

        // Audit log
        await auditService.LogActionAsync(
            "system",
            "ExecutiveMetricsRefresh",
            "ExecutiveKPI",
            "plant-wide",
            null,
            $"Refreshed executive metrics (today, WTD, MTD) and {deptRanking.Count} department rankings"
        );

        _logger.LogInformation("ExecutiveMetricsRefreshJob completed: Refreshed executive KPIs and {Count} department rankings", 
            deptRanking.Count);
    }
}
