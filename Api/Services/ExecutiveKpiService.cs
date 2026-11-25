using Microsoft.EntityFrameworkCore;
using WACAMS.Api.Data;
using WACAMS.Core.Enums;

namespace WACAMS.Api.Services;

/// <summary>
/// Executive-level KPI calculations with strategic insights and cross-departmental analytics.
/// Provides high-level metrics for C-suite and senior management decision-making.
/// </summary>
public class ExecutiveKpiService
{
    private readonly WacamDbContext _context;
    private readonly ILogger<ExecutiveKpiService> _logger;

    public ExecutiveKpiService(WacamDbContext context, ILogger<ExecutiveKpiService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get executive dashboard KPIs with strategic insights.
    /// </summary>
    public async Task<ExecutiveKpiDto> GetExecutiveKpisAsync(DateTime startDate, DateTime endDate)
    {
        // Get all active employees
        var totalEmployees = await _context.Employees.CountAsync(e => e.IsActive);
        
        // Get attendance records
        var records = await _context.AttendanceRecords
            .Where(r => r.Date >= startDate.Date && r.Date <= endDate.Date)
            .ToListAsync();

        // Get exceptions
        var exceptions = await _context.AttendanceExceptions
            .Where(e => e.Date >= startDate.Date && e.Date <= endDate.Date)
            .ToListAsync();

        // Get escalations
        var escalations = await _context.Escalations
            .Where(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate)
            .ToListAsync();

        // Get warnings
        var warnings = await _context.WarningLetters
            .Where(w => w.DateIssued >= startDate && w.DateIssued <= endDate)
            .ToListAsync();

        // Calculate core metrics
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
        var totalEscalations = escalations.Count;
        var escalationRate = totalExceptions > 0
            ? Math.Round((double)totalEscalations / totalExceptions * 100, 2)
            : 0.0;

        // Warning metrics
        var totalWarnings = warnings.Count;
        var warningIssuanceRate = investigatedExceptions > 0
            ? Math.Round((double)totalWarnings / investigatedExceptions * 100, 2)
            : 0.0;

        // Department ranking
        var departments = await _context.Departments.ToListAsync();
        var departmentRanking = new List<DepartmentPerformanceDto>();

        foreach (var dept in departments)
        {
            var deptEmployees = await _context.Employees
                .Where(e => e.DepartmentId == dept.Id && e.IsActive)
                .Select(e => e.EmployeeId)
                .ToListAsync();

            if (!deptEmployees.Any()) continue;

            var deptRecords = records.Where(r => deptEmployees.Contains(r.EmployeeId)).ToList();
            var deptPresentCount = deptRecords.Count(r => r.ClockInTime.HasValue);
            var deptAttendanceRate = deptEmployees.Count * totalDays > 0
                ? Math.Round((double)deptPresentCount / (deptEmployees.Count * totalDays) * 100, 2)
                : 100.0;

            var deptExceptions = exceptions.Where(e => deptEmployees.Contains(e.EmployeeId)).Count();
            var deptLostMinutes = deptRecords.Sum(r => r.LateMinutes + r.EarlyMinutes);

            departmentRanking.Add(new DepartmentPerformanceDto
            {
                DepartmentId = dept.Id,
                DepartmentName = dept.Name,
                AttendanceRate = deptAttendanceRate,
                TotalExceptions = deptExceptions,
                TotalLostMinutes = deptLostMinutes,
                TotalLostHours = Math.Round(deptLostMinutes / 60.0, 2)
            });
        }

        departmentRanking = departmentRanking.OrderByDescending(d => d.AttendanceRate).ToList();

        // Calculate cost impact (assuming average hourly rate)
        var averageHourlyRate = 150.00m; // ZAR per hour - configurable
        var totalLostHours = totalLostMinutes / 60.0;
        var estimatedCostImpact = (decimal)totalLostHours * averageHourlyRate;

        // Strategic insights
        var insights = GenerateStrategicInsights(
            attendanceRate, lateArrivalRate, absenteeismRate, 
            exceptionResolutionRate, escalationRate, departmentRanking);

        return new ExecutiveKpiDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalEmployees = totalEmployees,
            TotalDays = totalDays,
            AttendanceRate = attendanceRate,
            LateArrivalRate = lateArrivalRate,
            EarlyDepartureRate = earlyDepartureRate,
            AbsenteeismRate = absenteeismRate,
            TotalLostMinutes = totalLostMinutes,
            TotalLostHours = Math.Round(totalLostHours, 2),
            EstimatedCostImpact = estimatedCostImpact,
            TotalExceptions = totalExceptions,
            OpenInvestigations = openInvestigations,
            InvestigatedExceptions = investigatedExceptions,
            ExceptionResolutionRate = exceptionResolutionRate,
            TotalEscalations = totalEscalations,
            EscalationRate = escalationRate,
            TotalWarnings = totalWarnings,
            WarningIssuanceRate = warningIssuanceRate,
            DepartmentRanking = departmentRanking,
            TopPerformingDepartments = departmentRanking.Take(3).ToList(),
            BottomPerformingDepartments = departmentRanking.TakeLast(3).Reverse().ToList(),
            StrategicInsights = insights
        };
    }

    /// <summary>
    /// Get quarterly executive KPIs.
    /// </summary>
    public async Task<ExecutiveKpiDto> GetQuarterlyKpisAsync(int year, int quarter)
    {
        var quarterStartMonth = (quarter - 1) * 3 + 1;
        var startDate = new DateTime(year, quarterStartMonth, 1);
        var endDate = startDate.AddMonths(3).AddDays(-1);

        return await GetExecutiveKpisAsync(startDate, endDate);
    }

    /// <summary>
    /// Get year-to-date executive KPIs.
    /// </summary>
    public async Task<ExecutiveKpiDto> GetYtdKpisAsync(int year)
    {
        var startDate = new DateTime(year, 1, 1);
        var endDate = DateTime.Today;

        return await GetExecutiveKpisAsync(startDate, endDate);
    }

    private List<string> GenerateStrategicInsights(
        double attendanceRate, double lateArrivalRate, double absenteeismRate,
        double exceptionResolutionRate, double escalationRate,
        List<DepartmentPerformanceDto> departmentRanking)
    {
        var insights = new List<string>();

        // Attendance insights
        if (attendanceRate < 90)
            insights.Add($"Overall attendance rate ({attendanceRate}%) is below target of 90%. Immediate action required.");
        else if (attendanceRate < 95)
            insights.Add($"Attendance rate ({attendanceRate}%) is acceptable but below optimal level of 95%.");
        else
            insights.Add($"Excellent attendance rate of {attendanceRate}% across the organization.");

        // Late arrival insights
        if (lateArrivalRate > 5)
            insights.Add($"Late arrival rate ({lateArrivalRate}%) is concerning. Consider implementing flexible start times or addressing transportation issues.");

        // Absenteeism insights
        if (absenteeismRate > 3)
            insights.Add($"Absenteeism rate ({absenteeismRate}%) exceeds industry standard of 3%. Review leave policies and employee engagement.");

        // Investigation insights
        if (exceptionResolutionRate < 80)
            insights.Add($"Exception resolution rate ({exceptionResolutionRate}%) is below target. Managers need to prioritize timely investigations.");
        else if (exceptionResolutionRate > 95)
            insights.Add($"Excellent exception resolution rate of {exceptionResolutionRate}%. Investigation process is working effectively.");

        // Escalation insights
        if (escalationRate > 20)
            insights.Add($"High escalation rate ({escalationRate}%). Consider additional manager training on exception handling.");

        // Department performance insights
        if (departmentRanking.Any())
        {
            var top = departmentRanking.First();
            var bottom = departmentRanking.Last();
            insights.Add($"Top performing department: {top.DepartmentName} ({top.AttendanceRate}% attendance).");
            insights.Add($"Department requiring attention: {bottom.DepartmentName} ({bottom.AttendanceRate}% attendance).");
        }

        return insights;
    }
}

// DTOs
public record ExecutiveKpiDto
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int TotalEmployees { get; init; }
    public int TotalDays { get; init; }
    public double AttendanceRate { get; init; }
    public double LateArrivalRate { get; init; }
    public double EarlyDepartureRate { get; init; }
    public double AbsenteeismRate { get; init; }
    public int TotalLostMinutes { get; init; }
    public double TotalLostHours { get; init; }
    public decimal EstimatedCostImpact { get; init; }
    public int TotalExceptions { get; init; }
    public int OpenInvestigations { get; init; }
    public int InvestigatedExceptions { get; init; }
    public double ExceptionResolutionRate { get; init; }
    public int TotalEscalations { get; init; }
    public double EscalationRate { get; init; }
    public int TotalWarnings { get; init; }
    public double WarningIssuanceRate { get; init; }
    public List<DepartmentPerformanceDto> DepartmentRanking { get; init; } = new();
    public List<DepartmentPerformanceDto> TopPerformingDepartments { get; init; } = new();
    public List<DepartmentPerformanceDto> BottomPerformingDepartments { get; init; } = new();
    public List<string> StrategicInsights { get; init; } = new();
}

public record DepartmentPerformanceDto
{
    public Guid DepartmentId { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public double AttendanceRate { get; init; }
    public int TotalExceptions { get; init; }
    public int TotalLostMinutes { get; init; }
    public double TotalLostHours { get; init; }
}
