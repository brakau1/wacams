using Microsoft.EntityFrameworkCore;
using WACAMS.Api.Data;
using WACAMS.Core.Enums;

namespace WACAMS.Api.Services;

/// <summary>
/// Attendance analytics service — detailed attendance pattern analysis, anomaly detection, and workforce utilization.
/// </summary>
public class AttendanceAnalyticsService
{
    private readonly WacamDbContext _context;
    private readonly ILogger<AttendanceAnalyticsService> _logger;

    public AttendanceAnalyticsService(WacamDbContext context, ILogger<AttendanceAnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get attendance analytics for a date range with pattern analysis.
    /// </summary>
    public async Task<AttendanceAnalyticsDto> GetAttendanceAnalyticsAsync(DateTime startDate, DateTime endDate, Guid? departmentId = null)
    {
        _logger.LogDebug("Calculating attendance analytics for {StartDate} to {EndDate}", startDate, endDate);

        var query = _context.AttendanceRecords
            .Include(r => r.Employee)
                .ThenInclude(e => e!.Department)
            .Where(r => r.Date >= startDate.Date && r.Date <= endDate.Date);

        if (departmentId.HasValue)
            query = query.Where(r => r.Employee!.DepartmentId == departmentId.Value);

        var records = await query.ToListAsync();

        var totalEmployees = records.Select(r => r.EmployeeId).Distinct().Count();
        var totalDays = (endDate.Date - startDate.Date).Days + 1;
        var totalPossibleAttendances = totalEmployees * totalDays;

        var presentCount = records.Count(r => r.ClockInTime.HasValue);
        var lateCount = records.Count(r => r.Status == AttendanceStatus.Late);
        var earlyDepartureCount = records.Count(r => r.Status == AttendanceStatus.EarlyDeparture);
        var absentCount = totalPossibleAttendances - presentCount;
        var totalLateMinutes = records.Sum(r => r.LateMinutes);
        var totalEarlyMinutes = records.Sum(r => r.EarlyMinutes);
        var totalLostMinutes = totalLateMinutes + totalEarlyMinutes;

        // Day-of-week analysis
        var dayOfWeekAnalysis = records
            .GroupBy(r => r.Date.DayOfWeek)
            .Select(g => new DayOfWeekAnalyticsDto
            {
                DayOfWeek = g.Key.ToString(),
                TotalRecords = g.Count(),
                LateCount = g.Count(r => r.Status == AttendanceStatus.Late),
                AbsentCount = g.Count(r => !r.ClockInTime.HasValue),
                AvgLateMinutes = g.Where(r => r.LateMinutes > 0).DefaultIfEmpty().Average(r => r?.LateMinutes ?? 0)
            })
            .OrderBy(d => d.DayOfWeek)
            .ToList();

        // Shift analysis
        var shiftAnalysis = records
            .GroupBy(r => r.ShiftCode ?? "Unknown")
            .Select(g => new ShiftAnalyticsDto
            {
                ShiftCode = g.Key,
                TotalRecords = g.Count(),
                PresentCount = g.Count(r => r.ClockInTime.HasValue),
                LateCount = g.Count(r => r.Status == AttendanceStatus.Late),
                LostMinutes = g.Sum(r => r.LateMinutes + r.EarlyMinutes)
            })
            .ToList();

        // Top late offenders
        var topLateOffenders = records
            .Where(r => r.LateMinutes > 0)
            .GroupBy(r => r.EmployeeId)
            .Select(g => new LateOffenderDto
            {
                EmployeeId = g.Key,
                EmployeeName = g.First().Employee?.DisplayName ?? "Unknown",
                DepartmentName = g.First().Employee?.Department?.Name ?? "Unknown",
                LateCount = g.Count(),
                TotalLateMinutes = g.Sum(r => r.LateMinutes)
            })
            .OrderByDescending(o => o.TotalLateMinutes)
            .Take(20)
            .ToList();

        // Anomaly detection — employees with sudden pattern changes
        var anomalies = DetectAnomalies(records);

        return new AttendanceAnalyticsDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalEmployees = totalEmployees,
            TotalDays = totalDays,
            PresentCount = presentCount,
            LateCount = lateCount,
            EarlyDepartureCount = earlyDepartureCount,
            AbsentCount = absentCount,
            TotalLateMinutes = totalLateMinutes,
            TotalEarlyMinutes = totalEarlyMinutes,
            TotalLostMinutes = totalLostMinutes,
            AttendanceRate = totalPossibleAttendances > 0 ? Math.Round((double)presentCount / totalPossibleAttendances * 100, 2) : 0,
            OnTimeRate = presentCount > 0 ? Math.Round((double)(presentCount - lateCount - earlyDepartureCount) / presentCount * 100, 2) : 0,
            DayOfWeekAnalysis = dayOfWeekAnalysis,
            ShiftAnalysis = shiftAnalysis,
            TopLateOffenders = topLateOffenders,
            Anomalies = anomalies
        };
    }

    private List<AttendanceAnomalyDto> DetectAnomalies(List<WACAMS.Core.Entities.AttendanceRecord> records)
    {
        var anomalies = new List<AttendanceAnomalyDto>();

        // Group by employee and look for sudden changes
        var employeeGroups = records.GroupBy(r => r.EmployeeId);

        foreach (var group in employeeGroups)
        {
            var sorted = group.OrderBy(r => r.Date).ToList();
            if (sorted.Count < 5) continue;

            // Check for 3+ consecutive late arrivals
            var consecutiveLate = 0;
            foreach (var record in sorted)
            {
                if (record.Status == AttendanceStatus.Late)
                {
                    consecutiveLate++;
                    if (consecutiveLate >= 3)
                    {
                        anomalies.Add(new AttendanceAnomalyDto
                        {
                            EmployeeId = group.Key,
                            EmployeeName = record.Employee?.DisplayName ?? "Unknown",
                            AnomalyType = "ConsecutiveLateArrivals",
                            Description = $"{consecutiveLate}+ consecutive late arrivals",
                            Severity = consecutiveLate >= 5 ? "High" : "Medium",
                            DetectedDate = record.Date
                        });
                        break;
                    }
                }
                else
                {
                    consecutiveLate = 0;
                }
            }

            // Check for sudden increase in lost minutes
            var firstHalf = sorted.Take(sorted.Count / 2).Sum(r => r.LateMinutes + r.EarlyMinutes);
            var secondHalf = sorted.Skip(sorted.Count / 2).Sum(r => r.LateMinutes + r.EarlyMinutes);

            if (secondHalf > firstHalf * 2 && secondHalf > 60)
            {
                anomalies.Add(new AttendanceAnomalyDto
                {
                    EmployeeId = group.Key,
                    EmployeeName = sorted.First().Employee?.DisplayName ?? "Unknown",
                    AnomalyType = "IncreasedLostTime",
                    Description = $"Lost time increased from {firstHalf}m to {secondHalf}m",
                    Severity = "Medium",
                    DetectedDate = sorted.Last().Date
                });
            }
        }

        return anomalies;
    }
}

public class AttendanceAnalyticsDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalEmployees { get; set; }
    public int TotalDays { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int EarlyDepartureCount { get; set; }
    public int AbsentCount { get; set; }
    public int TotalLateMinutes { get; set; }
    public int TotalEarlyMinutes { get; set; }
    public int TotalLostMinutes { get; set; }
    public double AttendanceRate { get; set; }
    public double OnTimeRate { get; set; }
    public List<DayOfWeekAnalyticsDto> DayOfWeekAnalysis { get; set; } = new();
    public List<ShiftAnalyticsDto> ShiftAnalysis { get; set; } = new();
    public List<LateOffenderDto> TopLateOffenders { get; set; } = new();
    public List<AttendanceAnomalyDto> Anomalies { get; set; } = new();
}

public class DayOfWeekAnalyticsDto
{
    public string DayOfWeek { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public double AvgLateMinutes { get; set; }
}

public class ShiftAnalyticsDto
{
    public string ShiftCode { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int LostMinutes { get; set; }
}

public class LateOffenderDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int LateCount { get; set; }
    public int TotalLateMinutes { get; set; }
}

public class AttendanceAnomalyDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string AnomalyType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime DetectedDate { get; set; }
}
