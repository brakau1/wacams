using Microsoft.EntityFrameworkCore;
using Wacam.Api.Data;
using Wacam.Api.DTOs;
using WACAMS.Core.Entities;

namespace Wacam.Api.Services;

/// <summary>
/// Service for security analytics and metrics.
/// </summary>
public class SecurityAnalyticsService
{
    private readonly WacamDbContext _context;

    public SecurityAnalyticsService(WacamDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get comprehensive security analytics.
    /// </summary>
    public async Task<SecurityAnalyticsDto> GetAnalyticsAsync()
    {
        var analytics = new SecurityAnalyticsDto
        {
            UserGrowth = await GetUserGrowthAsync(),
            RoleDistribution = await GetRoleDistributionAsync(),
            OverrideUtilization = await GetOverrideUtilizationAsync(),
            SecurityEvents = await GetSecurityEventMetricsAsync(),
            AuditActivity = await GetAuditActivityAsync(),
            PermissionChanges = await GetPermissionChangeMetricsAsync()
        };

        return analytics;
    }

    private async Task<UserGrowthMetrics> GetUserGrowthAsync()
    {
        var totalUsers = await _context.Users.CountAsync();
        var activeUsers = await _context.Users.CountAsync(u => u.IsActive);

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfLastMonth = startOfMonth.AddMonths(-1);

        var newUsersThisMonth = await _context.Users
            .CountAsync(u => u.CreatedAt >= startOfMonth);

        var newUsersLastMonth = await _context.Users
            .CountAsync(u => u.CreatedAt >= startOfLastMonth && u.CreatedAt < startOfMonth);

        var growthRate = newUsersLastMonth > 0
            ? ((double)(newUsersThisMonth - newUsersLastMonth) / newUsersLastMonth) * 100
            : 0;

        // Generate trend data (last 12 months)
        var trend = new List<TrendDataPoint>();
        for (int i = 11; i >= 0; i--)
        {
            var monthStart = startOfMonth.AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);
            var count = await _context.Users
                .CountAsync(u => u.CreatedAt >= monthStart && u.CreatedAt < monthEnd);

            trend.Add(new TrendDataPoint
            {
                Label = monthStart.ToString("MMM yyyy"),
                Value = count
            });
        }

        return new UserGrowthMetrics
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            NewUsersThisMonth = newUsersThisMonth,
            NewUsersLastMonth = newUsersLastMonth,
            GrowthRate = growthRate,
            Trend = trend
        };
    }

    private async Task<RoleDistributionMetrics> GetRoleDistributionAsync()
    {
        var usersByRole = new Dictionary<string, int>();
        var percentageByRole = new Dictionary<string, double>();

        var roles = await _context.Roles.ToListAsync();
        var totalUsers = await _context.Users.CountAsync();

        foreach (var role in roles)
        {
            var count = await _context.UserRoles
                .CountAsync(ur => ur.RoleId == role.Id);

            usersByRole[role.Name] = count;
            percentageByRole[role.Name] = totalUsers > 0 ? (double)count / totalUsers * 100 : 0;
        }

        return new RoleDistributionMetrics
        {
            UsersByRole = usersByRole,
            PercentageByRole = percentageByRole
        };
    }

    private async Task<OverrideUtilizationMetrics> GetOverrideUtilizationAsync()
    {
        var totalOverrides = await _context.EmployeeAccessOverrides.CountAsync();
        var activeOverrides = await _context.EmployeeAccessOverrides
            .CountAsync(o => o.IsActive);
        var expiredOverrides = totalOverrides - activeOverrides;

        var utilizationRate = totalOverrides > 0
            ? (double)activeOverrides / totalOverrides * 100
            : 0;

        // Generate trend data (last 12 months)
        var trend = new List<TrendDataPoint>();
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        for (int i = 11; i >= 0; i--)
        {
            var monthStart = startOfMonth.AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);
            var count = await _context.EmployeeAccessOverrides
                .CountAsync(o => o.CreatedAt >= monthStart && o.CreatedAt < monthEnd);

            trend.Add(new TrendDataPoint
            {
                Label = monthStart.ToString("MMM yyyy"),
                Value = count
            });
        }

        return new OverrideUtilizationMetrics
        {
            TotalOverrides = totalOverrides,
            ActiveOverrides = activeOverrides,
            ExpiredOverrides = expiredOverrides,
            UtilizationRate = utilizationRate,
            Trend = trend
        };
    }

    private async Task<SecurityEventMetrics> GetSecurityEventMetricsAsync()
    {
        var totalEvents = await _context.SecurityEvents.CountAsync();
        var unacknowledgedEvents = await _context.SecurityEvents
            .CountAsync(e => !e.IsAcknowledged);

        var eventsByType = await _context.SecurityEvents
            .GroupBy(e => e.EventType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count);

        var eventsBySeverity = await _context.SecurityEvents
            .GroupBy(e => e.Severity)
            .Select(g => new { Severity = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Severity, x => x.Count);

        // Generate trend data (last 12 months)
        var trend = new List<TrendDataPoint>();
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        for (int i = 11; i >= 0; i--)
        {
            var monthStart = startOfMonth.AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);
            var count = await _context.SecurityEvents
                .CountAsync(e => e.OccurredAt >= monthStart && e.OccurredAt < monthEnd);

            trend.Add(new TrendDataPoint
            {
                Label = monthStart.ToString("MMM yyyy"),
                Value = count
            });
        }

        return new SecurityEventMetrics
        {
            TotalEvents = totalEvents,
            UnacknowledgedEvents = unacknowledgedEvents,
            EventsByType = eventsByType,
            EventsBySeverity = eventsBySeverity,
            Trend = trend
        };
    }

    private async Task<AuditActivityMetrics> GetAuditActivityAsync()
    {
        var totalActions = await _context.AuditLogs.CountAsync();

        var today = DateTime.UtcNow.Date;
        var actionsToday = await _context.AuditLogs
            .CountAsync(a => a.Timestamp >= today);

        var actionsByType = await _context.AuditLogs
            .GroupBy(a => a.Action)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Action, x => x.Count);

        // Generate trend data (last 12 months)
        var trend = new List<TrendDataPoint>();
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        for (int i = 11; i >= 0; i--)
        {
            var monthStart = startOfMonth.AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);
            var count = await _context.AuditLogs
                .CountAsync(a => a.Timestamp >= monthStart && a.Timestamp < monthEnd);

            trend.Add(new TrendDataPoint
            {
                Label = monthStart.ToString("MMM yyyy"),
                Value = count
            });
        }

        return new AuditActivityMetrics
        {
            TotalActions = totalActions,
            ActionsToday = actionsToday,
            ActionsByType = actionsByType,
            Trend = trend
        };
    }

    private async Task<PermissionChangeMetrics> GetPermissionChangeMetricsAsync()
    {
        var permissionActions = new[] { "PermissionChange", "RoleAssignment", "RoleRemoval", "OverrideCreated", "OverrideUpdated" };

        var totalChanges = await _context.AuditLogs
            .CountAsync(a => permissionActions.Contains(a.Action));

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        var changesThisMonth = await _context.AuditLogs
            .CountAsync(a => a.Timestamp >= startOfMonth && permissionActions.Contains(a.Action));

        var changesByType = await _context.AuditLogs
            .Where(a => permissionActions.Contains(a.Action))
            .GroupBy(a => a.Action)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Action, x => x.Count);

        // Generate trend data (last 12 months)
        var trend = new List<TrendDataPoint>();
        for (int i = 11; i >= 0; i--)
        {
            var monthStart = startOfMonth.AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);
            var count = await _context.AuditLogs
                .CountAsync(a => a.Timestamp >= monthStart && a.Timestamp < monthEnd && permissionActions.Contains(a.Action));

            trend.Add(new TrendDataPoint
            {
                Label = monthStart.ToString("MMM yyyy"),
                Value = count
            });
        }

        return new PermissionChangeMetrics
        {
            TotalChanges = totalChanges,
            ChangesThisMonth = changesThisMonth,
            ChangesByType = changesByType,
            Trend = trend
        };
    }
}
