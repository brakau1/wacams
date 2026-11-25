using Microsoft.EntityFrameworkCore;
using WACAMS.Api.Data;
using WACAMS.Core.Enums;

namespace WACAMS.Api.Services;

/// <summary>
/// Plant-wide performance analytics — aggregate metrics across all departments with production impact analysis.
/// </summary>
public class PlantPerformanceService
{
    private readonly WacamDbContext _context;
    private readonly ILogger<PlantPerformanceService> _logger;

    public PlantPerformanceService(WacamDbContext context, ILogger<PlantPerformanceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PlantPerformanceDto> GetPlantPerformanceAsync(DateTime startDate, DateTime endDate)
    {
        _logger.LogDebug("Calculating plant performance for {StartDate} to {EndDate}", startDate, endDate);

        var records = await _context.AttendanceRecords
            .Include(r => r.Employee)
                .ThenInclude(e => e!.Department)
            .Where(r => r.Date >= startDate.Date && r.Date <= endDate.Date)
            .ToListAsync();

        var exceptions = await _context.AttendanceExceptions
            .Where(e => e.Date >= startDate.Date && e.Date <= endDate.Date)
            .ToListAsync();

        var warnings = await _context.WarningLetters
            .Where(w => w.DateIssued >= startDate && w.DateIssued <= endDate)
            .ToListAsync();

        var escalations = await _context.Escalations
            .Where(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate)
            .ToListAsync();

        var totalEmployees = await _context.Employees.CountAsync(e => e.IsActive);
        var totalDays = (endDate.Date - startDate.Date).Days + 1;
        var totalPossibleAttendances = totalEmployees * totalDays;

        var presentCount = records.Count(r => r.ClockInTime.HasValue);
        var lateCount = records.Count(r => r.Status == AttendanceStatus.Late);
        var earlyDepartureCount = records.Count(r => r.Status == AttendanceStatus.EarlyDeparture);
        var absentCount = totalPossibleAttendances - presentCount;
        var totalLateMinutes = records.Sum(r => r.LateMinutes);
        var totalEarlyMinutes = records.Sum(r => r.EarlyMinutes);
        var totalLostMinutes = totalLateMinutes + totalEarlyMinutes;
        var lostHours = Math.Round(totalLostMinutes / 60.0, 1);

        // Convert lost minutes to estimated production units (assuming 1 unit per 2 minutes)
        var estimatedLostUnits = totalLostMinutes / 2;

        var attendanceRate = totalPossibleAttendances > 0
            ? Math.Round((double)presentCount / totalPossibleAttendances * 100, 2)
            : 0;

        var onTimeRate = presentCount > 0
            ? Math.Round((double)(presentCount - lateCount - earlyDepartureCount) / presentCount * 100, 2)
            : 0;

        // Daily breakdown
        var dailyPerformance = records
            .GroupBy(r => r.Date.Date)
            .Select(g => new DailyPerformanceDto
            {
                Date = g.Key,
                TotalEmployees = g.Select(r => r.EmployeeId).Distinct().Count(),
                PresentCount = g.Count(r => r.ClockInTime.HasValue),
                LateCount = g.Count(r => r.Status == AttendanceStatus.Late),
                AbsentCount = g.Select(r => r.EmployeeId).Distinct().Count() - g.Count(r => r.ClockInTime.HasValue),
                LostMinutes = g.Sum(r => r.LateMinutes + r.EarlyMinutes)
            })
            .OrderBy(d => d.Date)
            .ToList();

        // Department ranking
        var departmentRanking = records
            .GroupBy(r => r.Employee?.DepartmentId)
            .Select(g =>
            {
                var deptEmployees = g.Select(r => r.EmployeeId).Distinct().Count();
                var deptPresent = g.Count(r => r.ClockInTime.HasValue);
                return new PlantDeptRankingDto
                {
                    DepartmentId = g.Key,
                    DepartmentName = g.First().Employee?.Department?.Name ?? "Unknown",
                    TotalEmployees = deptEmployees,
                    AttendanceRate = deptEmployees > 0 ? Math.Round((double)deptPresent / (deptEmployees * totalDays) * 100, 2) : 0,
                    LostMinutes = g.Sum(r => r.LateMinutes + r.EarlyMinutes)
                };
            })
            .OrderByDescending(d => d.AttendanceRate)
            .ToList();

        // Overall health score (0-100)
        var healthScore = CalculateHealthScore(attendanceRate, onTimeRate, exceptions.Count, warnings.Count, escalations.Count);

        return new PlantPerformanceDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalEmployees = totalEmployees,
            TotalDays = totalDays,
            AttendanceRate = attendanceRate,
            OnTimeRate = onTimeRate,
            PresentCount = presentCount,
            LateCount = lateCount,
            EarlyDepartureCount = earlyDepartureCount,
            AbsentCount = absentCount,
            TotalLostMinutes = totalLostMinutes,
            LostHours = lostHours,
            EstimatedLostUnits = estimatedLostUnits,
            ExceptionCount = exceptions.Count,
            WarningCount = warnings.Count,
            EscalationCount = escalations.Count,
            HealthScore = healthScore,
            DailyPerformance = dailyPerformance,
            DepartmentRanking = departmentRanking
        };
    }

    private int CalculateHealthScore(double attendanceRate, double onTimeRate, int exceptions, int warnings, int escalations)
    {
        var score = 100;

        // Attendance impact (max -30)
        if (attendanceRate < 90) score -= 30;
        else if (attendanceRate < 95) score -= 15;
        else if (attendanceRate < 98) score -= 5;

        // On-time impact (max -20)
        if (onTimeRate < 80) score -= 20;
        else if (onTimeRate < 90) score -= 10;
        else if (onTimeRate < 95) score -= 5;

        // Exception impact (max -20)
        if (exceptions > 50) score -= 20;
        else if (exceptions > 20) score -= 10;
        else if (exceptions > 10) score -= 5;

        // Warning impact (max -15)
        if (warnings > 30) score -= 15;
        else if (warnings > 15) score -= 10;
        else if (warnings > 5) score -= 5;

        // Escalation impact (max -15)
        if (escalations > 20) score -= 15;
        else if (escalations > 10) score -= 10;
        else if (escalations > 5) score -= 5;

        return Math.Max(0, score);
    }
}

public class PlantPerformanceDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalEmployees { get; set; }
    public int TotalDays { get; set; }
    public double AttendanceRate { get; set; }
    public double OnTimeRate { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int EarlyDepartureCount { get; set; }
    public int AbsentCount { get; set; }
    public int TotalLostMinutes { get; set; }
    public double LostHours { get; set; }
    public double EstimatedLostUnits { get; set; }
    public int ExceptionCount { get; set; }
    public int WarningCount { get; set; }
    public int EscalationCount { get; set; }
    public int HealthScore { get; set; }
    public List<DailyPerformanceDto> DailyPerformance { get; set; } = new();
    public List<PlantDeptRankingDto> DepartmentRanking { get; set; } = new();
}

public class DailyPerformanceDto
{
    public DateTime Date { get; set; }
    public int TotalEmployees { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public int LostMinutes { get; set; }
}

public class PlantDeptRankingDto
{
    public Guid? DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalEmployees { get; set; }
    public double AttendanceRate { get; set; }
    public int LostMinutes { get; set; }
}
