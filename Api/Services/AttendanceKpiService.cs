using Microsoft.EntityFrameworkCore;
using Wacam.Api.Data;
using WACAMS.Core.Enums;

namespace Wacam.Api.Services;

/// <summary>
/// Calculates attendance KPIs for dashboards, reports, and analytics.
/// </summary>
public class AttendanceKpiService
{
    private readonly WacamDbContext _context;
    private readonly ILogger<AttendanceKpiService> _logger;

    public AttendanceKpiService(WacamDbContext context, ILogger<AttendanceKpiService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get comprehensive attendance KPIs for a date range.
    /// </summary>
    public async Task<AttendanceKpiDto> GetKpisAsync(DateTime startDate, DateTime endDate, Guid? departmentId = null)
    {
        var query = _context.AttendanceRecords
            .Include(r => r.Employee)
                .ThenInclude(e => e!.Department)
            .Where(r => r.Date >= startDate.Date && r.Date <= endDate.Date);

        if (departmentId.HasValue)
        {
            query = query.Where(r => r.Employee!.DepartmentId == departmentId.Value);
        }

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

        // Exception-based metrics
        var exceptionQuery = _context.AttendanceExceptions
            .Include(e => e.Employee)
            .Where(e => e.Date >= startDate.Date && e.Date <= endDate.Date);

        if (departmentId.HasValue)
        {
            exceptionQuery = exceptionQuery.Where(e => e.Employee!.DepartmentId == departmentId.Value);
        }

        var exceptions = await exceptionQuery.ToListAsync();

        var openInvestigations = exceptions.Count(e => e.InvestigationStatus == InvestigationStatus.Open || e.InvestigationStatus == InvestigationStatus.UnderReview);
        var investigatedExceptions = exceptions.Count(e => e.InvestigationStatus == InvestigationStatus.Investigated || e.InvestigationStatus == InvestigationStatus.Closed);
        var totalExceptions = exceptions.Count;

        var exceptionResolutionRate = totalExceptions > 0
            ? Math.Round((double)investigatedExceptions / totalExceptions * 100, 2)
            : 100.0;

        // Escalation metrics
        var escalationQuery = _context.Escalations
            .Include(e => e.Employee)
            .Where(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate);

        if (departmentId.HasValue)
        {
            escalationQuery = escalationQuery.Where(e => e.Employee!.DepartmentId == departmentId.Value);
        }

        var escalations = await escalationQuery.ToListAsync();
        var totalEscalations = escalations.Count;
        var resolvedEscalations = escalations.Count(e => e.Status == EscalationStatus.Closed || e.Status == EscalationStatus.ActionTaken);

        var escalationRate = totalExceptions > 0
            ? Math.Round((double)totalEscalations / totalExceptions * 100, 2)
            : 0.0;

        // Warning metrics
        var warningQuery = _context.WarningLetters
            .Include(w => w.Employee)
            .Where(w => w.DateIssued >= startDate && w.DateIssued <= endDate);

        if (departmentId.HasValue)
        {
            warningQuery = warningQuery.Where(w => w.Employee!.DepartmentId == departmentId.Value);
        }

        var warnings = await warningQuery.ToListAsync();
        var totalWarnings = warnings.Count;

        var warningIssuanceRate = investigatedExceptions > 0
            ? Math.Round((double)totalWarnings / investigatedExceptions * 100, 2)
            : 0.0;

        // Calculate rates
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

        return new AttendanceKpiDto
        {
            StartDate = startDate,
            EndDate = endDate,
            DepartmentId = departmentId,
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
            ResolvedEscalations = resolvedEscalations,
            EscalationRate = escalationRate,
            TotalWarnings = totalWarnings,
            WarningIssuanceRate = warningIssuanceRate
        };
    }

    /// <summary>
    /// Get department ranking by attendance performance.
    /// </summary>
    public async Task<List<DepartmentKpiDto>> GetDepartmentRankingAsync(DateTime startDate, DateTime endDate)
    {
        var departments = await _context.Departments.ToListAsync();
        var results = new List<DepartmentKpiDto>();

        foreach (var dept in departments)
        {
            var kpi = await GetKpisAsync(startDate, endDate, dept.Id);
            
            results.Add(new DepartmentKpiDto
            {
                DepartmentId = dept.Id,
                DepartmentName = dept.Name,
                TotalEmployees = kpi.TotalEmployees,
                AttendanceRate = kpi.AttendanceRate,
                LateArrivalRate = kpi.LateArrivalRate,
                EarlyDepartureRate = kpi.EarlyDepartureRate,
                AbsenteeismRate = kpi.AbsenteeismRate,
                TotalLostHours = kpi.TotalLostHours,
                TotalExceptions = kpi.TotalExceptions,
                OpenInvestigations = kpi.OpenInvestigations,
                ExceptionResolutionRate = kpi.ExceptionResolutionRate,
                TotalWarnings = kpi.TotalWarnings
            });
        }

        return results
            .OrderByDescending(d => d.AttendanceRate)
            .Select((d, index) =>
            {
                d.Rank = index + 1;
                return d;
            })
            .ToList();
    }

    /// <summary>
    /// Get monthly trend data for the last N months.
    /// </summary>
    public async Task<List<MonthlyKpiTrendDto>> GetMonthlyTrendsAsync(int monthsBack = 12, Guid? departmentId = null)
    {
        var trends = new List<MonthlyKpiTrendDto>();
        var today = DateTime.Today;

        for (int i = monthsBack - 1; i >= 0; i--)
        {
            var monthStart = today.AddMonths(-i).AddDays(-today.Day + 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var kpi = await GetKpisAsync(monthStart, monthEnd, departmentId);

            trends.Add(new MonthlyKpiTrendDto
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

    /// <summary>
    /// Get year-to-date KPIs.
    /// </summary>
    public async Task<AttendanceKpiDto> GetYtdKpisAsync(Guid? departmentId = null)
    {
        var yearStart = new DateTime(DateTime.Today.Year, 1, 1);
        var today = DateTime.Today;

        return await GetKpisAsync(yearStart, today, departmentId);
    }
}

// DTOs
public record AttendanceKpiDto
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public Guid? DepartmentId { get; init; }
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
    public int ResolvedEscalations { get; init; }
    public double EscalationRate { get; init; }
    public int TotalWarnings { get; init; }
    public double WarningIssuanceRate { get; init; }
}

public record DepartmentKpiDto
{
    public Guid DepartmentId { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public int Rank { get; set; }
    public int TotalEmployees { get; init; }
    public double AttendanceRate { get; init; }
    public double LateArrivalRate { get; init; }
    public double EarlyDepartureRate { get; init; }
    public double AbsenteeismRate { get; init; }
    public double TotalLostHours { get; init; }
    public int TotalExceptions { get; init; }
    public int OpenInvestigations { get; init; }
    public double ExceptionResolutionRate { get; init; }
    public int TotalWarnings { get; init; }
}

public record MonthlyKpiTrendDto
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
