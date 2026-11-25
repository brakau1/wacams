using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WACAMS.Api.Data;

namespace WACAMS.Api.Services;

/// <summary>
/// Dashboard aggregation service with multi-layer caching for optimal performance.
/// Aggregates data from multiple sources into unified dashboard payloads.
/// </summary>
public class DashboardAggregationService
{
    private readonly WacamDbContext _context;
    private readonly AttendanceKpiService _attendanceKpi;
    private readonly DepartmentKpiService _departmentKpi;
    private readonly ExecutiveKpiService _executiveKpi;
    private readonly ILogger<DashboardAggregationService> _logger;
    private readonly MemoryCache _cache;
    private readonly CacheItemPolicy _cachePolicy;

    public DashboardAggregationService(
        WacamDbContext context,
        AttendanceKpiService attendanceKpi,
        DepartmentKpiService departmentKpi,
        ExecutiveKpiService executiveKpi,
        ILogger<DashboardAggregationService> logger)
    {
        _context = context;
        _attendanceKpi = attendanceKpi;
        _departmentKpi = departmentKpi;
        _executiveKpi = executiveKpi;
        _logger = logger;
        _cache = MemoryCache.Default;
        _cachePolicy = new CacheItemPolicy
        {
            AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(60) // 60-second cache TTL
        };
    }

    /// <summary>
    /// Get aggregated dashboard data with caching.
    /// </summary>
    public async Task<DashboardAggregationDto> GetDashboardAsync(string dashboardType, DateTime? startDate = null, DateTime? endDate = null)
    {
        var cacheKey = $"dashboard_{dashboardType}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

        if (_cache.Contains(cacheKey))
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return (DashboardAggregationDto)_cache.Get(cacheKey)!;
        }

        _logger.LogDebug("Cache miss for {CacheKey}, computing...", cacheKey);

        var start = startDate ?? DateTime.Today;
        var end = endDate ?? DateTime.Today;

        var result = dashboardType.ToLower() switch
        {
            "executive" => await GetExecutiveDashboardAsync(start, end),
            "manager" => await GetManagerDashboardAsync(start, end),
            "hr" => await GetHRDashboardAsync(start, end),
            "department" => await GetDepartmentDashboardAsync(start, end),
            _ => await GetDefaultDashboardAsync(start, end)
        };

        _cache.Set(cacheKey, result, _cachePolicy);
        return result;
    }

    /// <summary>
    /// Get KPI data with caching.
    /// </summary>
    public async Task<KpiAggregationDto> GetKpisAsync(string kpiType, DateTime startDate, DateTime endDate, Guid? departmentId = null)
    {
        var cacheKey = $"kpi_{kpiType}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}_{departmentId}";

        if (_cache.Contains(cacheKey))
        {
            _logger.LogDebug("KPI cache hit for {CacheKey}", cacheKey);
            return (KpiAggregationDto)_cache.Get(cacheKey)!;
        }

        var result = kpiType.ToLower() switch
        {
            "attendance" => await GetAttendanceKpisAsync(startDate, endDate, departmentId),
            "department" => await GetDepartmentKpisAsync(startDate, endDate, departmentId),
            "executive" => await GetExecutiveKpisAsync(startDate, endDate),
            _ => await GetAttendanceKpisAsync(startDate, endDate, departmentId)
        };

        _cache.Set(cacheKey, result, _cachePolicy);
        return result;
    }

    /// <summary>
    /// Get department analytics with caching.
    /// </summary>
    public async Task<DepartmentAggregationDto> GetDepartmentAnalyticsAsync(Guid departmentId, DateTime startDate, DateTime endDate)
    {
        var cacheKey = $"dept_analytics_{departmentId}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

        if (_cache.Contains(cacheKey))
        {
            _logger.LogDebug("Department cache hit for {CacheKey}", cacheKey);
            return (DepartmentAggregationDto)_cache.Get(cacheKey)!;
        }

        var records = await _context.AttendanceRecords
            .Include(r => r.Employee)
                .ThenInclude(e => e!.Department)
            .Where(r => r.Employee!.DepartmentId == departmentId && r.Date >= startDate.Date && r.Date <= endDate.Date)
            .ToListAsync();

        var totalEmployees = records.Select(r => r.EmployeeId).Distinct().Count();
        var presentCount = records.Count(r => r.ClockInTime.HasValue);
        var lateCount = records.Count(r => r.Status == WACAMS.Core.Enums.AttendanceStatus.Late);
        var lostMinutes = records.Sum(r => r.LateMinutes + r.EarlyMinutes);

        var result = new DepartmentAggregationDto
        {
            DepartmentId = departmentId,
            DepartmentName = records.FirstOrDefault()?.Employee?.Department?.Name ?? "Unknown",
            TotalEmployees = totalEmployees,
            PresentCount = presentCount,
            LateCount = lateCount,
            TotalLostMinutes = lostMinutes,
            AttendanceRate = totalEmployees > 0 ? Math.Round((double)presentCount / (totalEmployees * ((endDate - startDate).Days + 1)) * 100, 2) : 0
        };

        _cache.Set(cacheKey, result, _cachePolicy);
        return result;
    }

    /// <summary>
    /// Invalidate all caches (called after data mutations).
    /// </summary>
    public void InvalidateCache()
    {
        _logger.LogInformation("Invalidating all dashboard caches");
        foreach (var item in _cache)
        {
            _cache.Remove(item.Key);
        }
    }

    /// <summary>
    /// Invalidate specific cache key.
    /// </summary>
    public void InvalidateCache(string cacheKey)
    {
        _logger.LogInformation("Invalidating cache key: {CacheKey}", cacheKey);
        _cache.Remove(cacheKey);
    }

    private async Task<DashboardAggregationDto> GetExecutiveDashboardAsync(DateTime startDate, DateTime endDate)
    {
        var kpis = await _executiveKpi.GetExecutiveKpisAsync(startDate, endDate);
        var heatmap = await _executiveKpi.GetDepartmentHeatmapAsync(DateTime.Today);
        var leaderboard = await _executiveKpi.GetDepartmentLeaderboardAsync(startDate, endDate);

        return new DashboardAggregationDto
        {
            DashboardType = "Executive",
            StartDate = startDate,
            EndDate = endDate,
            Summary = new DashboardSummaryDto
            {
                TotalEmployees = kpis.TotalEmployees,
                AttendanceRate = kpis.AttendanceRate,
                OnTimeRate = kpis.OnTimeRate,
                TotalLostMinutes = kpis.TotalLostMinutes,
                ExceptionCount = kpis.ExceptionCount,
                EscalationCount = kpis.EscalationCount,
                WarningCount = kpis.WarningCount
            },
            HeatmapData = heatmap,
            LeaderboardData = leaderboard,
            GeneratedAt = DateTime.UtcNow
        };
    }

    private async Task<DashboardAggregationDto> GetManagerDashboardAsync(DateTime startDate, DateTime endDate)
    {
        var attendanceKpis = await _attendanceKpi.GetKpisAsync(startDate, endDate);
        var departmentRanking = await _departmentKpi.GetDepartmentRankingAsync(startDate, endDate);

        return new DashboardAggregationDto
        {
            DashboardType = "Manager",
            StartDate = startDate,
            EndDate = endDate,
            Summary = new DashboardSummaryDto
            {
                TotalEmployees = attendanceKpis.TotalEmployees,
                AttendanceRate = attendanceKpis.AttendanceRate,
                OnTimeRate = attendanceKpis.OnTimeRate,
                TotalLostMinutes = attendanceKpis.TotalLateMinutes + attendanceKpis.TotalEarlyMinutes,
                ExceptionCount = attendanceKpis.ExceptionCount,
                EscalationCount = 0,
                WarningCount = 0
            },
            DepartmentRanking = departmentRanking,
            GeneratedAt = DateTime.UtcNow
        };
    }

    private async Task<DashboardAggregationDto> GetHRDashboardAsync(DateTime startDate, DateTime endDate)
    {
        var attendanceKpis = await _attendanceKpi.GetKpisAsync(startDate, endDate);
        var departmentRanking = await _departmentKpi.GetDepartmentRankingAsync(startDate, endDate);

        return new DashboardAggregationDto
        {
            DashboardType = "HR",
            StartDate = startDate,
            EndDate = endDate,
            Summary = new DashboardSummaryDto
            {
                TotalEmployees = attendanceKpis.TotalEmployees,
                AttendanceRate = attendanceKpis.AttendanceRate,
                OnTimeRate = attendanceKpis.OnTimeRate,
                TotalLostMinutes = attendanceKpis.TotalLateMinutes + attendanceKpis.TotalEarlyMinutes,
                ExceptionCount = attendanceKpis.ExceptionCount,
                EscalationCount = 0,
                WarningCount = 0
            },
            DepartmentRanking = departmentRanking,
            GeneratedAt = DateTime.UtcNow
        };
    }

    private async Task<DashboardAggregationDto> GetDepartmentDashboardAsync(DateTime startDate, DateTime endDate)
    {
        var attendanceKpis = await _attendanceKpi.GetKpisAsync(startDate, endDate);

        return new DashboardAggregationDto
        {
            DashboardType = "Department",
            StartDate = startDate,
            EndDate = endDate,
            Summary = new DashboardSummaryDto
            {
                TotalEmployees = attendanceKpis.TotalEmployees,
                AttendanceRate = attendanceKpis.AttendanceRate,
                OnTimeRate = attendanceKpis.OnTimeRate,
                TotalLostMinutes = attendanceKpis.TotalLateMinutes + attendanceKpis.TotalEarlyMinutes,
                ExceptionCount = attendanceKpis.ExceptionCount,
                EscalationCount = 0,
                WarningCount = 0
            },
            GeneratedAt = DateTime.UtcNow
        };
    }

    private async Task<DashboardAggregationDto> GetDefaultDashboardAsync(DateTime startDate, DateTime endDate)
    {
        return await GetManagerDashboardAsync(startDate, endDate);
    }

    private async Task<KpiAggregationDto> GetAttendanceKpisAsync(DateTime startDate, DateTime endDate, Guid? departmentId)
    {
        var kpis = await _attendanceKpi.GetKpisAsync(startDate, endDate, departmentId);

        return new KpiAggregationDto
        {
            KpiType = "Attendance",
            StartDate = startDate,
            EndDate = endDate,
            TotalEmployees = kpis.TotalEmployees,
            AttendanceRate = kpis.AttendanceRate,
            OnTimeRate = kpis.OnTimeRate,
            LateCount = kpis.LateCount,
            EarlyDepartureCount = kpis.EarlyDepartureCount,
            AbsentCount = kpis.AbsentCount,
            TotalLostMinutes = kpis.TotalLateMinutes + kpis.TotalEarlyMinutes,
            ExceptionCount = kpis.ExceptionCount
        };
    }

    private async Task<KpiAggregationDto> GetDepartmentKpisAsync(DateTime startDate, DateTime endDate, Guid? departmentId)
    {
        var kpis = await _departmentKpi.GetDepartmentKpisAsync(startDate, endDate, departmentId);

        return new KpiAggregationDto
        {
            KpiType = "Department",
            StartDate = startDate,
            EndDate = endDate,
            TotalEmployees = kpis.TotalEmployees,
            AttendanceRate = kpis.AttendanceRate,
            OnTimeRate = kpis.OnTimeRate,
            LateCount = kpis.LateCount,
            EarlyDepartureCount = kpis.EarlyDepartureCount,
            AbsentCount = kpis.AbsentCount,
            TotalLostMinutes = kpis.TotalLostMinutes,
            ExceptionCount = kpis.ExceptionCount
        };
    }

    private async Task<KpiAggregationDto> GetExecutiveKpisAsync(DateTime startDate, DateTime endDate)
    {
        var kpis = await _executiveKpi.GetExecutiveKpisAsync(startDate, endDate);

        return new KpiAggregationDto
        {
            KpiType = "Executive",
            StartDate = startDate,
            EndDate = endDate,
            TotalEmployees = kpis.TotalEmployees,
            AttendanceRate = kpis.AttendanceRate,
            OnTimeRate = kpis.OnTimeRate,
            LateCount = kpis.LateCount,
            EarlyDepartureCount = kpis.EarlyDepartureCount,
            AbsentCount = kpis.AbsentCount,
            TotalLostMinutes = kpis.TotalLostMinutes,
            ExceptionCount = kpis.ExceptionCount,
            EscalationCount = kpis.EscalationCount,
            WarningCount = kpis.WarningCount
        };
    }
}

public class DashboardAggregationDto
{
    public string DashboardType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DashboardSummaryDto Summary { get; set; } = new();
    public List<object>? HeatmapData { get; set; }
    public List<object>? LeaderboardData { get; set; }
    public List<object>? DepartmentRanking { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class DashboardSummaryDto
{
    public int TotalEmployees { get; set; }
    public double AttendanceRate { get; set; }
    public double OnTimeRate { get; set; }
    public int TotalLostMinutes { get; set; }
    public int ExceptionCount { get; set; }
    public int EscalationCount { get; set; }
    public int WarningCount { get; set; }
}

public class KpiAggregationDto
{
    public string KpiType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalEmployees { get; set; }
    public double AttendanceRate { get; set; }
    public double OnTimeRate { get; set; }
    public int LateCount { get; set; }
    public int EarlyDepartureCount { get; set; }
    public int AbsentCount { get; set; }
    public int TotalLostMinutes { get; set; }
    public int ExceptionCount { get; set; }
    public int EscalationCount { get; set; }
    public int WarningCount { get; set; }
}

public class DepartmentAggregationDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalEmployees { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int TotalLostMinutes { get; set; }
    public double AttendanceRate { get; set; }
}
