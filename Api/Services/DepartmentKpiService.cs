using Microsoft.EntityFrameworkCore;
using Wacam.Api.Data;
using WACAMS.Core.Enums;

namespace Wacam.Api.Services;

/// <summary>
/// Department-specific KPI calculations and analytics.
/// Provides detailed metrics for department managers and supervisors.
/// </summary>
public class DepartmentKpiService
{
    private readonly WacamDbContext _context;
    private readonly ILogger<DepartmentKpiService> _logger;

    public DepartmentKpiService(WacamDbContext context, ILogger<DepartmentKpiService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get comprehensive department KPIs with employee breakdown.
    /// </summary>
    public async Task<DepartmentKpiDetailDto> GetDepartmentKpisAsync(Guid departmentId, DateTime startDate, DateTime endDate)
    {
        var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == departmentId);
        if (department == null)
            throw new KeyNotFoundException($"Department {departmentId} not found");

        var employees = await _context.Employees
            .Where(e => e.DepartmentId == departmentId && e.IsActive)
            .Select(e => e.EmployeeId)
            .ToListAsync();

        if (!employees.Any())
        {
            return new DepartmentKpiDetailDto
            {
                DepartmentId = departmentId,
                DepartmentName = department.Name,
                StartDate = startDate,
                EndDate = endDate,
                TotalEmployees = 0,
                EmployeeBreakdown = new List<EmployeeKpiDto>()
            };
        }

        // Get attendance records
        var records = await _context.AttendanceRecords
            .Where(r => employees.Contains(r.EmployeeId) && r.Date >= startDate.Date && r.Date <= endDate.Date)
            .ToListAsync();

        // Get exceptions
        var exceptions = await _context.AttendanceExceptions
            .Where(e => employees.Contains(e.EmployeeId) && e.Date >= startDate.Date && e.Date <= endDate.Date)
            .ToListAsync();

        // Calculate per-employee KPIs
        var employeeBreakdown = new List<EmployeeKpiDto>();
        foreach (var empId in employees)
        {
            var empRecords = records.Where(r => r.EmployeeId == empId).ToList();
            var empExceptions = exceptions.Where(e => e.EmployeeId == empId).ToList();

            var totalDays = empRecords.Count;
            var presentDays = empRecords.Count(r => r.ClockInTime.HasValue);
            var lateDays = empRecords.Count(r => r.Status == AttendanceStatus.Late);
            var earlyDepartureDays = empRecords.Count(r => r.Status == AttendanceStatus.EarlyDeparture);
            var absentDays = totalDays - presentDays;
            var totalLateMinutes = empRecords.Sum(r => r.LateMinutes);
            var totalEarlyMinutes = empRecords.Sum(r => r.EarlyMinutes);
            var totalLostMinutes = totalLateMinutes + totalEarlyMinutes;

            var attendanceRate = totalDays > 0
                ? Math.Round((double)presentDays / totalDays * 100, 2)
                : 100.0;

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == empId);

            employeeBreakdown.Add(new EmployeeKpiDto
            {
                EmployeeId = empId,
                DisplayName = employee?.DisplayName ?? empId,
                TotalDays = totalDays,
                PresentDays = presentDays,
                LateDays = lateDays,
                EarlyDepartureDays = earlyDepartureDays,
                AbsentDays = absentDays,
                AttendanceRate = attendanceRate,
                TotalLateMinutes = totalLateMinutes,
                TotalEarlyMinutes = totalEarlyMinutes,
                TotalLostMinutes = totalLostMinutes,
                TotalExceptions = empExceptions.Count,
                OpenInvestigations = empExceptions.Count(e => e.InvestigationStatus == InvestigationStatus.Open || e.InvestigationStatus == InvestigationStatus.UnderReview)
            });
        }

        // Aggregate department-level metrics
        var totalEmployees = employees.Count;
        var totalDays = (endDate.Date - startDate.Date).Days + 1;
        var totalPossibleAttendances = totalEmployees * totalDays;
        var presentCount = records.Count(r => r.ClockInTime.HasValue);
        var lateCount = records.Count(r => r.Status == AttendanceStatus.Late);
        var earlyDepartureCount = records.Count(r => r.Status == AttendanceStatus.EarlyDeparture);
        var absentCount = totalPossibleAttendances - presentCount;
        var totalLostMinutes = records.Sum(r => r.LateMinutes + r.EarlyMinutes);

        var attendanceRate = totalPossibleAttendances > 0
            ? Math.Round((double)presentCount / totalPossibleAttendances * 100, 2)
            : 100.0;

        var lateArrivalRate = totalPossibleAttendances > 0
            ? Math.Round((double)lateCount / totalPossibleAttendances * 100, 2)
            : 0.0;

        var earlyDepartureRate = totalPossibleAttendances > 0
            ? Math.Round((double)earlyDepartureCount / totalPossibleAttendances * 100, 2)
            : 0.0;

        var absenteeismRate = totalPossibleAttendances > 0
            ? Math.Round((double)absentCount / totalPossibleAttendances * 100, 2)
            : 0.0;

        // Exception metrics
        var totalExceptions = exceptions.Count;
        var openInvestigations = exceptions.Count(e => e.InvestigationStatus == InvestigationStatus.Open || e.InvestigationStatus == InvestigationStatus.UnderReview);
        var investigatedExceptions = exceptions.Count(e => e.InvestigationStatus == InvestigationStatus.Investigated || e.InvestigationStatus == InvestigationStatus.Closed);

        var exceptionResolutionRate = totalExceptions > 0
            ? Math.Round((double)investigatedExceptions / totalExceptions * 100, 2)
            : 100.0;

        // Escalation metrics
        var escalations = await _context.Escalations
            .Where(e => employees.Contains(e.EmployeeId) && e.CreatedAt >= startDate && e.CreatedAt <= endDate)
            .ToListAsync();

        var totalEscalations = escalations.Count;
        var escalationRate = totalExceptions > 0
            ? Math.Round((double)totalEscalations / totalExceptions * 100, 2)
            : 0.0;

        // Warning metrics
        var warnings = await _context.WarningLetters
            .Where(w => employees.Contains(w.EmployeeId) && w.DateIssued >= startDate && w.DateIssued <= endDate)
            .ToListAsync();

        var totalWarnings = warnings.Count;
        var warningIssuanceRate = investigatedExceptions > 0
            ? Math.Round((double)totalWarnings / investigatedExceptions * 100, 2)
            : 0.0;

        return new DepartmentKpiDetailDto
        {
            DepartmentId = departmentId,
            DepartmentName = department.Name,
            StartDate = startDate,
            EndDate = endDate,
            TotalEmployees = totalEmployees,
            TotalDays = totalDays,
            TotalPossibleAttendances = totalPossibleAttendances,
            PresentCount = presentCount,
            LateCount = lateCount,
            EarlyDepartureCount = earlyDepartureCount,
            AbsentCount = absentCount,
            AttendanceRate = attendanceRate,
            LateArrivalRate = lateArrivalRate,
            EarlyDepartureRate = earlyDepartureRate,
            AbsenteeismRate = absenteeismRate,
            TotalLostMinutes = totalLostMinutes,
            TotalLostHours = Math.Round(totalLostMinutes / 60.0, 2),
            TotalExceptions = totalExceptions,
            OpenInvestigations = openInvestigations,
            InvestigatedExceptions = investigatedExceptions,
            ExceptionResolutionRate = exceptionResolutionRate,
            TotalEscalations = totalEscalations,
            EscalationRate = escalationRate,
            TotalWarnings = totalWarnings,
            WarningIssuanceRate = warningIssuanceRate,
            EmployeeBreakdown = employeeBreakdown.OrderByDescending(e => e.AttendanceRate).ToList()
        };
    }

    /// <summary>
    /// Get department trend data for the last N months.
    /// </summary>
    public async Task<List<DepartmentMonthlyTrendDto>> GetDepartmentMonthlyTrendsAsync(Guid departmentId, int monthsBack = 12)
    {
        var trends = new List<DepartmentMonthlyTrendDto>();
        var today = DateTime.Today;

        for (int i = monthsBack - 1; i >= 0; i--)
        {
            var monthStart = today.AddMonths(-i).AddDays(-today.Day + 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var kpi = await GetDepartmentKpisAsync(departmentId, monthStart, monthEnd);

            trends.Add(new DepartmentMonthlyTrendDto
            {
                Month = monthStart.ToString("yyyy-MM"),
                AttendanceRate = kpi.AttendanceRate,
                LateArrivalRate = kpi.LateArrivalRate,
                EarlyDepartureRate = kpi.EarlyDepartureRate,
                AbsenteeismRate = kpi.AbsenteeismRate,
                TotalLostHours = kpi.TotalLostHours,
                TotalExceptions = kpi.TotalExceptions,
                OpenInvestigations = kpi.OpenInvestigations,
                ExceptionResolutionRate = kpi.ExceptionResolutionRate,
                EscalationRate = kpi.EscalationRate,
                TotalWarnings = kpi.TotalWarnings
            });
        }

        return trends;
    }
}

// DTOs
public record DepartmentKpiDetailDto
{
    public Guid DepartmentId { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int TotalEmployees { get; init; }
    public int TotalDays { get; init; }
    public int TotalPossibleAttendances { get; init; }
    public int PresentCount { get; init; }
    public int LateCount { get; init; }
    public int EarlyDepartureCount { get; init; }
    public int AbsentCount { get; init; }
    public double AttendanceRate { get; init; }
    public double LateArrivalRate { get; init; }
    public double EarlyDepartureRate { get; init; }
    public double AbsenteeismRate { get; init; }
    public int TotalLostMinutes { get; init; }
    public double TotalLostHours { get; init; }
    public int TotalExceptions { get; init; }
    public int OpenInvestigations { get; init; }
    public int InvestigatedExceptions { get; init; }
    public double ExceptionResolutionRate { get; init; }
    public int TotalEscalations { get; init; }
    public double EscalationRate { get; init; }
    public int TotalWarnings { get; init; }
    public double WarningIssuanceRate { get; init; }
    public List<EmployeeKpiDto> EmployeeBreakdown { get; init; } = new();
}

public record EmployeeKpiDto
{
    public string EmployeeId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public int TotalDays { get; init; }
    public int PresentDays { get; init; }
    public int LateDays { get; init; }
    public int EarlyDepartureDays { get; init; }
    public int AbsentDays { get; init; }
    public double AttendanceRate { get; init; }
    public int TotalLateMinutes { get; init; }
    public int TotalEarlyMinutes { get; init; }
    public int TotalLostMinutes { get; init; }
    public int TotalExceptions { get; init; }
    public int OpenInvestigations { get; init; }
}

public record DepartmentMonthlyTrendDto
{
    public string Month { get; init; } = string.Empty;
    public double AttendanceRate { get; init; }
    public double LateArrivalRate { get; init; }
    public double EarlyDepartureRate { get; init; }
    public double AbsenteeismRate { get; init; }
    public double TotalLostHours { get; init; }
    public int TotalExceptions { get; init; }
    public int OpenInvestigations { get; init; }
    public double ExceptionResolutionRate { get; init; }
    public double EscalationRate { get; init; }
    public int TotalWarnings { get; init; }
}
