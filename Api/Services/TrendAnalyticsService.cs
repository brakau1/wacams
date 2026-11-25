using Microsoft.EntityFrameworkCore;
using WACAMS.Api.Data;
using WACAMS.Core.Enums;

namespace WACAMS.Api.Services;

/// <summary>
/// Trend analytics service — multi-period trend analysis for attendance, absenteeism, lost production,
/// corrective actions, and escalations. Supports 7-day, 30-day, 90-day, quarterly, and YTD trends.
/// </summary>
public class TrendAnalyticsService
{
    private readonly WacamDbContext _context;
    private readonly ILogger<TrendAnalyticsService> _logger;

    public TrendAnalyticsService(WacamDbContext context, ILogger<TrendAnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get comprehensive trend analysis for all metrics across a specified period.
    /// </summary>
    public async Task<TrendAnalyticsDto> GetTrendsAsync(string period, Guid? departmentId = null)
    {
        var (startDate, endDate, granularity) = ResolvePeriod(period);
        _logger.LogDebug("Calculating trends for period {Period} ({StartDate} to {EndDate})", period, startDate, endDate);

        var attendanceTrend = await GetAttendanceTrendAsync(startDate, endDate, granularity, departmentId);
        var absenteeismTrend = await GetAbsenteeismTrendAsync(startDate, endDate, granularity, departmentId);
        var lostProductionTrend = await GetLostProductionTrendAsync(startDate, endDate, granularity, departmentId);
        var correctiveActionTrend = await GetCorrectiveActionTrendAsync(startDate, endDate, granularity, departmentId);
        var escalationTrend = await GetEscalationTrendAsync(startDate, endDate, granularity, departmentId);

        return new TrendAnalyticsDto
        {
            Period = period,
            StartDate = startDate,
            EndDate = endDate,
            Granularity = granularity,
            AttendanceTrend = attendanceTrend,
            AbsenteeismTrend = absenteeismTrend,
            LostProductionTrend = lostProductionTrend,
            CorrectiveActionTrend = correctiveActionTrend,
            EscalationTrend = escalationTrend
        };
    }

    /// <summary>
    /// Get attendance rate trend over time.
    /// </summary>
    public async Task<List<TrendPointDto>> GetAttendanceTrendAsync(DateTime startDate, DateTime endDate, string granularity, Guid? departmentId = null)
    {
        var query = _context.AttendanceRecords
            .Include(r => r.Employee)
            .Where(r => r.Date >= startDate.Date && r.Date <= endDate.Date);

        if (departmentId.HasValue)
            query = query.Where(r => r.Employee!.DepartmentId == departmentId.Value);

        var records = await query.ToListAsync();

        var grouped = GroupByGranularity(records, r => r.Date.Date, granularity);

        return grouped.Select(g =>
        {
            var totalEmployees = g.Select(r => r.EmployeeId).Distinct().Count();
            var presentCount = g.Count(r => r.ClockInTime.HasValue);
            var totalDays = GetDaysInGroup(g, r => r.Date.Date, granularity);
            var totalPossible = totalEmployees * totalDays;

            return new TrendPointDto
            {
                Period = g.Key,
                Value = totalPossible > 0 ? Math.Round((double)presentCount / totalPossible * 100, 2) : 0,
                Count = presentCount,
                Total = totalPossible
            };
        }).OrderBy(t => t.Period).ToList();
    }

    /// <summary>
    /// Get absenteeism trend over time.
    /// </summary>
    public async Task<List<TrendPointDto>> GetAbsenteeismTrendAsync(DateTime startDate, DateTime endDate, string granularity, Guid? departmentId = null)
    {
        var query = _context.AttendanceRecords
            .Include(r => r.Employee)
            .Where(r => r.Date >= startDate.Date && r.Date <= endDate.Date);

        if (departmentId.HasValue)
            query = query.Where(r => r.Employee!.DepartmentId == departmentId.Value);

        var records = await query.ToListAsync();

        var grouped = GroupByGranularity(records, r => r.Date.Date, granularity);

        return grouped.Select(g =>
        {
            var totalEmployees = g.Select(r => r.EmployeeId).Distinct().Count();
            var absentCount = g.GroupBy(r => new { r.EmployeeId, Date = r.Date.Date })
                .Count(gr => !gr.Any(r => r.ClockInTime.HasValue));

            return new TrendPointDto
            {
                Period = g.Key,
                Value = absentCount,
                Count = absentCount,
                Total = totalEmployees
            };
        }).OrderBy(t => t.Period).ToList();
    }

    /// <summary>
    /// Get lost production trend (in minutes) over time.
    /// </summary>
    public async Task<List<TrendPointDto>> GetLostProductionTrendAsync(DateTime startDate, DateTime endDate, string granularity, Guid? departmentId = null)
    {
        var query = _context.AttendanceRecords
            .Include(r => r.Employee)
            .Where(r => r.Date >= startDate.Date && r.Date <= endDate.Date);

        if (departmentId.HasValue)
            query = query.Where(r => r.Employee!.DepartmentId == departmentId.Value);

        var records = await query.ToListAsync();

        var grouped = GroupByGranularity(records, r => r.Date.Date, granularity);

        return grouped.Select(g => new TrendPointDto
        {
            Period = g.Key,
            Value = g.Sum(r => r.LateMinutes + r.EarlyMinutes),
            Count = g.Count(r => r.LateMinutes > 0 || r.EarlyMinutes > 0),
            Total = g.Sum(r => r.LateMinutes + r.EarlyMinutes)
        }).OrderBy(t => t.Period).ToList();
    }

    /// <summary>
    /// Get corrective action (warning) trend over time.
    /// </summary>
    public async Task<List<TrendPointDto>> GetCorrectiveActionTrendAsync(DateTime startDate, DateTime endDate, string granularity, Guid? departmentId = null)
    {
        var query = _context.WarningLetters
            .Include(w => w.Employee)
            .Where(w => w.DateIssued >= startDate && w.DateIssued <= endDate);

        if (departmentId.HasValue)
            query = query.Where(w => w.Employee!.DepartmentId == departmentId.Value);

        var warnings = await query.ToListAsync();

        var grouped = GroupByGranularity(warnings, w => w.DateIssued.Date, granularity);

        return grouped.Select(g => new TrendPointDto
        {
            Period = g.Key,
            Value = g.Count(),
            Count = g.Count(),
            Total = g.Count()
        }).OrderBy(t => t.Period).ToList();
    }

    /// <summary>
    /// Get escalation trend over time.
    /// </summary>
    public async Task<List<TrendPointDto>> GetEscalationTrendAsync(DateTime startDate, DateTime endDate, string granularity, Guid? departmentId = null)
    {
        var query = _context.Escalations
            .Include(e => e.Employee)
            .Where(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate);

        if (departmentId.HasValue)
            query = query.Where(e => e.Employee!.DepartmentId == departmentId.Value);

        var escalations = await query.ToListAsync();

        var grouped = GroupByGranularity(escalations, e => e.CreatedAt.Date, granularity);

        return grouped.Select(g => new TrendPointDto
        {
            Period = g.Key,
            Value = g.Count(),
            Count = g.Count(),
            Total = g.Count()
        }).OrderBy(t => t.Period).ToList();
    }

    private (DateTime StartDate, DateTime EndDate, string Granularity) ResolvePeriod(string period)
    {
        var today = DateTime.Today;

        return period.ToLower() switch
        {
            "7d" or "7day" or "7-day" => (today.AddDays(-7), today, "day"),
            "30d" or "30day" or "30-day" => (today.AddDays(-30), today, "day"),
            "90d" or "90day" or "90-day" => (today.AddDays(-90), today, "week"),
            "quarterly" or "quarter" => (today.AddMonths(-3), today, "week"),
            "ytd" or "year" => (new DateTime(today.Year, 1, 1), today, "month"),
            _ => (today.AddDays(-30), today, "day")
        };
    }

    private IEnumerable<IGrouping<string, T>> GroupByGranularity<T>(
        IEnumerable<T> items,
        Func<T, DateTime> dateSelector,
        string granularity)
    {
        return granularity.ToLower() switch
        {
            "day" => items.GroupBy(i => dateSelector(i).ToString("yyyy-MM-dd")),
            "week" => items.GroupBy(i =>
            {
                var date = dateSelector(i);
                var startOfWeek = date.AddDays(-(int)date.DayOfWeek);
                return startOfWeek.ToString("yyyy-MM-dd");
            }),
            "month" => items.GroupBy(i => dateSelector(i).ToString("yyyy-MM")),
            "quarter" => items.GroupBy(i =>
            {
                var date = dateSelector(i);
                var quarter = (date.Month - 1) / 3 + 1;
                return $"{date.Year}-Q{quarter}";
            }),
            _ => items.GroupBy(i => dateSelector(i).ToString("yyyy-MM-dd"))
        };
    }

    private int GetDaysInGroup<T>(IEnumerable<T> group, Func<T, DateTime> dateSelector, string granularity)
    {
        return granularity.ToLower() switch
        {
            "day" => 1,
            "week" => 7,
            "month" => DateTime.DaysInMonth(group.First() is not null ? dateSelector(group.First()).Year : DateTime.Today.Year,
                                             group.First() is not null ? dateSelector(group.First()).Month : 1),
            "quarter" => 90,
            _ => 1
        };
    }
}

public class TrendAnalyticsDto
{
    public string Period { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Granularity { get; set; } = string.Empty;
    public List<TrendPointDto> AttendanceTrend { get; set; } = new();
    public List<TrendPointDto> AbsenteeismTrend { get; set; } = new();
    public List<TrendPointDto> LostProductionTrend { get; set; } = new();
    public List<TrendPointDto> CorrectiveActionTrend { get; set; } = new();
    public List<TrendPointDto> EscalationTrend { get; set; } = new();
}

public class TrendPointDto
{
    public string Period { get; set; } = string.Empty;
    public double Value { get; set; }
    public int Count { get; set; }
    public int Total { get; set; }
}
