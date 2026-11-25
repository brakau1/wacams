using Microsoft.EntityFrameworkCore;
using WACAMS.Api.Data;
using WACAMS.Core.Enums;

namespace WACAMS.Api.Services;

/// <summary>
/// Executive-level analytics service providing strategic insights and cross-departmental analysis.
/// </summary>
public class ExecutiveAnalyticsService
{
    private readonly WacamDbContext _context;
    private readonly ILogger<ExecutiveAnalyticsService> _logger;

    public ExecutiveAnalyticsService(WacamDbContext context, ILogger<ExecutiveAnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get executive summary with strategic KPIs and insights.
    /// </summary>
    public async Task<ExecutiveAnalyticsDto> GetExecutiveSummaryAsync(DateTime startDate, DateTime endDate)
    {
        _logger.LogDebug("Calculating executive analytics for {StartDate} to {EndDate}", startDate, endDate);

        var employees = await _context.Employees
            .Where(e => e.IsActive)
            .ToListAsync();

        var attendanceRecords = await _context.AttendanceRecords
            .Include(r => r.Employee)
                .ThenInclude(e => e!.Department)
            .Where(r => r.Date >= startDate.Date && r.Date <= endDate.Date)
            .ToListAsync();

        var exceptions = await _context.AttendanceExceptions
            .Include(e => e.Employee)
                .ThenInclude(e => e!.Department)
            .Where(e => e.Date >= startDate.Date && e.Date <= endDate.Date)
            .ToListAsync();

        var escalations = await _context.Escalations
            .Where(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate)
            .ToListAsync();

        var warnings = await _context.WarningLetters
            .Where(w => w.DateIssued >= startDate && w.DateIssued <= endDate)
            .ToListAsync();

        var totalEmployees = employees.Count;
        var totalDays = (endDate.Date - startDate.Date).Days + 1;
        var totalPossibleAttendances = totalEmployees * totalDays;

        var presentCount = attendanceRecords.Count(r => r.ClockInTime.HasValue);
        var lateCount = attendanceRecords.Count(r => r.Status == AttendanceStatus.Late);
        var earlyDepartureCount = attendanceRecords.Count(r => r.Status == AttendanceStatus.EarlyDeparture);
        var absentCount = totalPossibleAttendances - presentCount;
        var totalLostMinutes = attendanceRecords.Sum(r => r.LateMinutes + r.EarlyMinutes);

        var attendanceRate = totalPossibleAttendances > 0
            ? Math.Round((double)presentCount / totalPossibleAttendances * 100, 2)
            : 0;

        var onTimeRate = presentCount > 0
            ? Math.Round((double)(presentCount - lateCount - earlyDepartureCount) / presentCount * 100, 2)
            : 0;

        // Department performance
        var departmentPerformance = attendanceRecords
            .GroupBy(r => r.Employee?.DepartmentId)
            .Select(g =>
            {
                var deptRecords = g.ToList();
                var deptPresent = deptRecords.Count(r => r.ClockInTime.HasValue);
                var deptLate = deptRecords.Count(r => r.Status == AttendanceStatus.Late);
                var deptLostMinutes = deptRecords.Sum(r => r.LateMinutes + r.EarlyMinutes);

                return new DepartmentPerformanceDto
                {
                    DepartmentId = g.Key,
                    DepartmentName = g.First().Employee?.Department?.Name ?? "Unknown",
                    AttendanceRate = deptRecords.Count > 0
                        ? Math.Round((double)deptPresent / deptRecords.Count * 100, 2)
                        : 0,
                    LateCount = deptLate,
                    LostMinutes = deptLostMinutes
                };
            })
            .OrderByDescending(d => d.AttendanceRate)
            .ToList();

        return new ExecutiveAnalyticsDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalEmployees = totalEmployees,
            AttendanceRate = attendanceRate,
            OnTimeRate = onTimeRate,
            LateCount = lateCount,
            EarlyDepartureCount = earlyDepartureCount,
            AbsentCount = absentCount,
            TotalLostMinutes = totalLostMinutes,
            ExceptionCount = exceptions.Count,
            EscalationCount = escalations.Count,
            WarningCount = warnings.Count,
            DepartmentPerformance = departmentPerformance
        };
    }

    /// <summary>
    /// Get department heatmap data for executive dashboard.
    /// </summary>
    public async Task<List<DepartmentHeatmapDto>> GetDepartmentHeatmapAsync(DateTime date)
    {
        var departments = await _context.Departments.ToListAsync();
        var heatmap = new List<DepartmentHeatmapDto>();

        foreach (var dept in departments)
        {
            var records = await _context.AttendanceRecords
                .Include(r => r.Employee)
                .Where(r => r.Employee!.DepartmentId == dept.Id && r.Date == date.Date)
                .ToListAsync();

            var totalEmployees = records.Select(r => r.EmployeeId).Distinct().Count();
            var presentCount = records.Count(r => r.ClockInTime.HasValue);
            var lateCount = records.Count(r => r.Status == AttendanceStatus.Late);
            var absentCount = totalEmployees - presentCount;
            var lostMinutes = records.Sum(r => r.LateMinutes + r.EarlyMinutes);

            var attendanceRate = totalEmployees > 0
                ? Math.Round((double)presentCount / totalEmployees * 100, 2)
                : 0;

            heatmap.Add(new DepartmentHeatmapDto
            {
                DepartmentId = dept.Id,
                DepartmentName = dept.Name,
                DepartmentCode = dept.Code,
                TotalEmployees = totalEmployees,
                PresentCount = presentCount,
                LateCount = lateCount,
                AbsentCount = absentCount,
                LostMinutes = lostMinutes,
                AttendanceRate = attendanceRate,
                HeatLevel = GetHeatLevel(attendanceRate)
            });
        }

        return heatmap.OrderByDescending(h => h.AttendanceRate).ToList();
    }

    private string GetHeatLevel(double attendanceRate)
    {
        return attendanceRate switch
        {
            >= 95 => "green",
            >= 90 => "yellow",
            >= 85 => "orange",
            _ => "red"
        };
    }
}

public class ExecutiveAnalyticsDto
{
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
    public List<DepartmentPerformanceDto> DepartmentPerformance { get; set; } = new();
}

public class DepartmentPerformanceDto
{
    public Guid? DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public double AttendanceRate { get; set; }
    public int LateCount { get; set; }
    public int LostMinutes { get; set; }
}

public class DepartmentHeatmapDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string DepartmentCode { get; set; } = string.Empty;
    public int TotalEmployees { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public int LostMinutes { get; set; }
    public double AttendanceRate { get; set; }
    public string HeatLevel { get; set; } = string.Empty;
}
