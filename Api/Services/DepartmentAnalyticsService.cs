using Microsoft.EntityFrameworkCore;
using WACAMS.Api.Data;
using WACAMS.Core.Enums;

namespace WACAMS.Api.Services;

/// <summary>
/// Department analytics — department-level performance metrics, comparisons, and health scoring.
/// </summary>
public class DepartmentAnalyticsService
{
    private readonly WacamDbContext _context;
    private readonly ILogger<DepartmentAnalyticsService> _logger;

    public DepartmentAnalyticsService(WacamDbContext context, ILogger<DepartmentAnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DepartmentAnalyticsDto> GetDepartmentAnalyticsAsync(Guid departmentId, DateTime startDate, DateTime endDate)
    {
        _logger.LogDebug("Calculating department analytics for department {DepartmentId}", departmentId);

        var department = await _context.Departments.FindAsync(departmentId);
        if (department == null)
            throw new KeyNotFoundException($"Department {departmentId} not found");

        var records = await _context.AttendanceRecords
            .Include(r => r.Employee)
            .Where(r => r.Employee!.DepartmentId == departmentId && r.Date >= startDate.Date && r.Date <= endDate.Date)
            .ToListAsync();

        var exceptions = await _context.AttendanceExceptions
            .Include(e => e.Employee)
            .Where(e => e.Employee!.DepartmentId == departmentId && e.Date >= startDate.Date && e.Date <= endDate.Date)
            .ToListAsync();

        var warnings = await _context.WarningLetters
            .Include(w => w.Employee)
            .Where(w => w.Employee!.DepartmentId == departmentId && w.DateIssued >= startDate && w.DateIssued <= endDate)
            .ToListAsync();

        var totalEmployees = records.Select(r => r.EmployeeId).Distinct().Count();
        var totalDays = (endDate.Date - startDate.Date).Days + 1;
        var totalPossibleAttendances = totalEmployees * totalDays;

        var presentCount = records.Count(r => r.ClockInTime.HasValue);
        var lateCount = records.Count(r => r.Status == AttendanceStatus.Late);
        var earlyDepartureCount = records.Count(r => r.Status == AttendanceStatus.EarlyDeparture);
        var absentCount = totalPossibleAttendances - presentCount;
        var totalLostMinutes = records.Sum(r => r.LateMinutes + r.EarlyMinutes);

        var attendanceRate = totalPossibleAttendances > 0
            ? Math.Round((double)presentCount / totalPossibleAttendances * 100, 2)
            : 0;

        // Employee rankings within department
        var employeeRankings = records
            .GroupBy(r => r.EmployeeId)
            .Select(g => new EmployeeRankingDto
            {
                EmployeeId = g.Key,
                EmployeeName = g.First().Employee?.FullName ?? "Unknown",
                AttendanceRate = g.Count > 0
                    ? Math.Round((double)g.Count(r => r.ClockInTime.HasValue) / g.Count() * 100, 2)
                    : 0,
                LateCount = g.Count(r => r.Status == AttendanceStatus.Late),
                TotalLostMinutes = g.Sum(r => r.LateMinutes + r.EarlyMinutes)
            })
            .OrderByDescending(e => e.AttendanceRate)
            .Take(10)
            .ToList();

        // Risk assessment
        var riskLevel = CalculateRiskLevel(attendanceRate, exceptions.Count, warnings.Count);

        return new DepartmentAnalyticsDto
        {
            DepartmentId = departmentId,
            DepartmentName = department.Name,
            StartDate = startDate,
            EndDate = endDate,
            TotalEmployees = totalEmployees,
            AttendanceRate = attendanceRate,
            PresentCount = presentCount,
            LateCount = lateCount,
            EarlyDepartureCount = earlyDepartureCount,
            AbsentCount = absentCount,
            TotalLostMinutes = totalLostMinutes,
            ExceptionCount = exceptions.Count,
            WarningCount = warnings.Count,
            RiskLevel = riskLevel,
            EmployeeRankings = employeeRankings
        };
    }

    public async Task<List<DepartmentComparisonDto>> CompareDepartmentsAsync(DateTime startDate, DateTime endDate)
    {
        var departments = await _context.Departments.ToListAsync();
        var comparisons = new List<DepartmentComparisonDto>();

        foreach (var dept in departments)
        {
            var records = await _context.AttendanceRecords
                .Include(r => r.Employee)
                .Where(r => r.Employee!.DepartmentId == dept.Id && r.Date >= startDate.Date && r.Date <= endDate.Date)
                .ToListAsync();

            var totalEmployees = records.Select(r => r.EmployeeId).Distinct().Count();
            var presentCount = records.Count(r => r.ClockInTime.HasValue);
            var lostMinutes = records.Sum(r => r.LateMinutes + r.EarlyMinutes);

            var attendanceRate = totalEmployees > 0
                ? Math.Round((double)presentCount / totalEmployees * 100, 2)
                : 0;

            comparisons.Add(new DepartmentComparisonDto
            {
                DepartmentId = dept.Id,
                DepartmentName = dept.Name,
                TotalEmployees = totalEmployees,
                AttendanceRate = attendanceRate,
                TotalLostMinutes = lostMinutes,
                Rank = 0 // Will be set after sorting
            });
        }

        // Assign ranks
        var ranked = comparisons.OrderByDescending(c => c.AttendanceRate).ToList();
        for (int i = 0; i < ranked.Count; i++)
            ranked[i].Rank = i + 1;

        return ranked;
    }

    private string CalculateRiskLevel(double attendanceRate, int exceptionCount, int warningCount)
    {
        var riskScore = 0;

        if (attendanceRate < 85) riskScore += 3;
        else if (attendanceRate < 90) riskScore += 2;
        else if (attendanceRate < 95) riskScore += 1;

        if (exceptionCount > 20) riskScore += 3;
        else if (exceptionCount > 10) riskScore += 2;
        else if (exceptionCount > 5) riskScore += 1;

        if (warningCount > 10) riskScore += 3;
        else if (warningCount > 5) riskScore += 2;
        else if (warningCount > 2) riskScore += 1;

        return riskScore switch
        {
            >= 7 => "Critical",
            >= 4 => "High",
            >= 2 => "Medium",
            _ => "Low"
        };
    }
}

public class DepartmentAnalyticsDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalEmployees { get; set; }
    public double AttendanceRate { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int EarlyDepartureCount { get; set; }
    public int AbsentCount { get; set; }
    public int TotalLostMinutes { get; set; }
    public int ExceptionCount { get; set; }
    public int WarningCount { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public List<EmployeeRankingDto> EmployeeRankings { get; set; } = new();
}

public class EmployeeRankingDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public double AttendanceRate { get; set; }
    public int LateCount { get; set; }
    public int TotalLostMinutes { get; set; }
}

public class DepartmentComparisonDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalEmployees { get; set; }
    public double AttendanceRate { get; set; }
    public int TotalLostMinutes { get; set; }
    public int Rank { get; set; }
}
