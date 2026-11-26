using Microsoft.EntityFrameworkCore;
using WACAMS.Api.Data;
using WACAMS.Core.Enums;
using WACAMS.Api.Middleware;

namespace WACAMS.Api.Services;

/// <summary>
/// Aggregates data for dashboard views — executive, manager, supervisor, and employee levels.
/// </summary>
public class DashboardService
{
    private readonly WacamDbContext _context;
    private readonly AttendanceKpiService _kpiService;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        WacamDbContext context,
        AttendanceKpiService kpiService,
        ILogger<DashboardService> logger)
    {
        _context = context;
        _kpiService = kpiService;
        _logger = logger;
    }

    /// <summary>
    /// Get executive dashboard overview — high-level KPIs and alerts.
    /// </summary>
    public async Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync()
    {
        var today = DateTime.Today;
        var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
        var thisMonthStart = new DateTime(today.Year, today.Month, 1);

        // Today's snapshot
        var todayKpi = await _kpiService.GetKpisAsync(today, today);

        // Week-to-date
        var wtdKpi = await _kpiService.GetKpisAsync(thisWeekStart, today);

        // Month-to-date
        var mtdKpi = await _kpiService.GetKpisAsync(thisMonthStart, today);

        // Critical alerts
        var criticalAlerts = await GetCriticalAlertsAsync();

        // Department ranking (top 5 and bottom 5)
        var deptRanking = await _kpiService.GetDepartmentRankingAsync(thisMonthStart, today);
        var topDepartments = deptRanking.Take(5).ToList();
        var bottomDepartments = deptRanking.TakeLast(5).Reverse().ToList();

        return new ExecutiveDashboardDto
        {
            Date = today,
            TodaySnapshot = todayKpi,
            WeekToDateSnapshot = wtdKpi,
            MonthToDateSnapshot = mtdKpi,
            CriticalAlerts = criticalAlerts,
            TopPerformingDepartments = topDepartments,
            BottomPerformingDepartments = bottomDepartments,
            TotalDepartments = deptRanking.Count
        };
    }

    /// <summary>
    /// Get manager dashboard — department-specific view with team details.
    /// </summary>
    public async Task<ManagerDashboardDto> GetManagerDashboardAsync(Guid departmentId)
    {
        var today = DateTime.Today;
        var thisMonthStart = new DateTime(today.Year, today.Month, 1);

        var dept = await _context.Departments.FirstOrDefaultAsync(d => d.Id == departmentId);
        if (dept == null)
            throw new NotFoundException($"Department {departmentId} not found.");

        // Department KPIs
        var deptKpi = await _kpiService.GetKpisAsync(thisMonthStart, today, departmentId);

        // Employee breakdown
        var employees = await _context.Employees
            .Where(e => e.DepartmentId == departmentId && e.IsActive)
            .Select(e => new EmployeeSummaryDto
            {
                EmployeeId = e.EmployeeId,
                DisplayName = e.DisplayName,
                Email = e.Email
            })
            .ToListAsync();

        // Recent exceptions (last 30 days)
        var recentExceptions = await _context.AttendanceExceptions
            .Include(e => e.Employee)
            .Where(e => e.Employee!.DepartmentId == departmentId && e.Date >= today.AddDays(-30))
            .OrderByDescending(e => e.Date)
            .Take(50)
            .Select(e => new ExceptionSummaryDto
            {
                Id = e.Id,
                EmployeeId = e.EmployeeId,
                EmployeeName = e.Employee!.DisplayName,
                Date = e.Date,
                ExceptionType = e.ExceptionType.ToString(),
                Status = e.InvestigationStatus,
                LostMinutes = e.LostProductionMinutes
            })
            .ToListAsync();

        // Open investigations
        var openInvestigations = recentExceptions
            .Where(e => e.Status == InvestigationStatus.Open || e.Status == InvestigationStatus.UnderReview)
            .Count();

        // Pending escalations
        var pendingEscalations = await _context.Escalations
            .Include(e => e.Employee)
            .Where(e => e.Employee!.DepartmentId == departmentId && 
                       (e.Status == EscalationStatus.Pending || e.Status == EscalationStatus.Accepted))
            .CountAsync();

        return new ManagerDashboardDto
        {
            DepartmentId = departmentId,
            DepartmentName = dept.Name,
            MonthToDateKpi = deptKpi,
            TotalEmployees = employees.Count,
            Employees = employees,
            RecentExceptions = recentExceptions,
            OpenInvestigations = openInvestigations,
            PendingEscalations = pendingEscalations
        };
    }

    /// <summary>
    /// Get supervisor dashboard — team-specific view with direct reports.
    /// </summary>
    public async Task<SupervisorDashboardDto> GetSupervisorDashboardAsync(string supervisorEmployeeId)
    {
        var today = DateTime.Today;
        var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);

        var supervisor = await _context.Employees
            .FirstOrDefaultAsync(e => e.EmployeeId == supervisorEmployeeId);

        if (supervisor == null)
            throw new NotFoundException($"Supervisor {supervisorEmployeeId} not found.");

        // Get direct reports (employees reporting to this supervisor)
        var directReports = await _context.Employees
            .Where(e => e.ManagerId == supervisorEmployeeId && e.IsActive)
            .Select(e => e.EmployeeId)
            .ToListAsync();

        if (!directReports.Any())
        {
            return new SupervisorDashboardDto
            {
                SupervisorEmployeeId = supervisorEmployeeId,
                SupervisorName = supervisor.DisplayName,
                DirectReports = new List<string>(),
                WeekToDateKpi = null,
                TodayAttendance = new List<TodayAttendanceDto>(),
                PendingInvestigations = new List<ExceptionSummaryDto>(),
                PendingEscalations = new List<EscalationSummaryDto>()
            };
        }

        // Week-to-date KPI for team
        var teamKpi = await GetTeamKpiAsync(directReports, thisWeekStart, today);

        // Today's attendance for direct reports
        var todayAttendance = await _context.AttendanceRecords
            .Include(r => r.Employee)
            .Where(r => directReports.Contains(r.EmployeeId) && r.Date == today)
            .Select(r => new TodayAttendanceDto
            {
                EmployeeId = r.EmployeeId,
                EmployeeName = r.Employee!.DisplayName,
                ClockIn = r.ClockInTime,
                ClockOut = r.ClockOutTime,
                IsLate = r.Status == AttendanceStatus.Late,
                IsEarlyDeparture = r.Status == AttendanceStatus.EarlyDeparture,
                MinutesLate = r.LateMinutes,
                MinutesEarly = r.EarlyMinutes,
                Status = r.Status
            })
            .ToListAsync();

        // Pending investigations (exceptions assigned to supervisor's team)
        var pendingInvestigations = await _context.AttendanceExceptions
            .Include(e => e.Employee)
            .Where(e => directReports.Contains(e.EmployeeId) && 
                       (e.InvestigationStatus == InvestigationStatus.Open || e.InvestigationStatus == InvestigationStatus.UnderReview))
            .OrderBy(e => e.Date)
            .Select(e => new ExceptionSummaryDto
            {
                Id = e.Id,
                EmployeeId = e.EmployeeId,
                EmployeeName = e.Employee!.DisplayName,
                Date = e.Date,
                ExceptionType = e.ExceptionType.ToString(),
                Status = e.InvestigationStatus,
                LostMinutes = e.LostProductionMinutes
            })
            .ToListAsync();

        // Pending escalations
        var pendingEscalations = await _context.Escalations
            .Include(e => e.Employee)
            .Where(e => e.ToManagerId == supervisorEmployeeId && 
                       (e.Status == EscalationStatus.Pending || e.Status == EscalationStatus.Accepted))
            .Select(e => new EscalationSummaryDto
            {
                Id = e.Id,
                EmployeeId = e.EmployeeId,
                EmployeeName = e.Employee!.DisplayName,
                Level = e.Level,
                Status = e.Status,
                CreatedAt = e.CreatedAt,
                ActionRequired = e.ActionRequired
            })
            .ToListAsync();

        return new SupervisorDashboardDto
        {
            SupervisorEmployeeId = supervisorEmployeeId,
            SupervisorName = supervisor.DisplayName,
            DirectReports = directReports,
            WeekToDateKpi = teamKpi,
            TodayAttendance = todayAttendance,
            PendingInvestigations = pendingInvestigations,
            PendingEscalations = pendingEscalations
        };
    }

    /// <summary>
    /// Get employee dashboard — personal attendance view.
    /// </summary>
    public async Task<EmployeeDashboardDto> GetEmployeeDashboardAsync(string employeeId)
    {
        var today = DateTime.Today;
        var thisMonthStart = new DateTime(today.Year, today.Month, 1);

        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

        if (employee == null)
            throw new NotFoundException($"Employee {employeeId} not found.");

        // Personal KPIs for this month
        var records = await _context.AttendanceRecords
            .Where(r => r.EmployeeId == employeeId && r.Date >= thisMonthStart && r.Date <= today)
            .ToListAsync();

        var totalDays = records.Count;
        var presentDays = records.Count(r => r.ClockInTime.HasValue);
        var lateDays = records.Count(r => r.Status == AttendanceStatus.Late);
        var earlyDepartureDays = records.Count(r => r.Status == AttendanceStatus.EarlyDeparture);
        var totalLateMinutes = records.Sum(r => r.LateMinutes);
        var totalEarlyMinutes = records.Sum(r => r.EarlyMinutes);

        var attendanceRate = totalDays > 0
            ? Math.Round((double)presentDays / totalDays * 100, 2)
            : 100.0;

        // Recent exceptions
        var recentExceptions = await _context.AttendanceExceptions
            .Where(e => e.EmployeeId == employeeId && e.Date >= today.AddDays(-90))
            .OrderByDescending(e => e.Date)
            .Take(20)
            .Select(e => new ExceptionSummaryDto
            {
                Id = e.Id,
                EmployeeId = e.EmployeeId,
                EmployeeName = employee.DisplayName,
                Date = e.Date,
                ExceptionType = e.ExceptionType.ToString(),
                Status = e.InvestigationStatus,
                LostMinutes = e.LostProductionMinutes
            })
            .ToListAsync();

        // Warnings received
        var warnings = await _context.WarningLetters
            .Where(w => w.EmployeeId == employeeId)
            .OrderByDescending(w => w.DateIssued)
            .Take(10)
            .Select(w => new WarningSummaryDto
            {
                Id = w.Id,
                Type = w.Type,
                DateIssued = w.DateIssued,
                Reason = w.Reason,
                Signed = w.Signed
            })
            .ToListAsync();

        // Today's record
        var todayRecord = await _context.AttendanceRecords
            .FirstOrDefaultAsync(r => r.EmployeeId == employeeId && r.Date == today);

        return new EmployeeDashboardDto
        {
            EmployeeId = employeeId,
            DisplayName = employee.DisplayName,
            DepartmentName = employee.Department?.Name ?? string.Empty,
            MonthToDateAttendanceRate = attendanceRate,
            MonthToDatePresentDays = presentDays,
            MonthToDateLateDays = lateDays,
            MonthToDateEarlyDepartureDays = earlyDepartureDays,
            MonthToDateTotalLateMinutes = totalLateMinutes,
            MonthToDateTotalEarlyMinutes = totalEarlyMinutes,
            TodayRecord = todayRecord != null ? new TodayAttendanceDto
            {
                EmployeeId = todayRecord.EmployeeId,
                EmployeeName = employee.DisplayName,
                ClockIn = todayRecord.ClockInTime,
                ClockOut = todayRecord.ClockOutTime,
                IsLate = todayRecord.Status == AttendanceStatus.Late,
                IsEarlyDeparture = todayRecord.Status == AttendanceStatus.EarlyDeparture,
                MinutesLate = todayRecord.LateMinutes,
                MinutesEarly = todayRecord.EarlyMinutes,
                Status = todayRecord.Status
            } : null,
            RecentExceptions = recentExceptions,
            Warnings = warnings
        };
    }

    private async Task<AttendanceKpiDto> GetTeamKpiAsync(List<string> employeeIds, DateTime startDate, DateTime endDate)
    {
        var records = await _context.AttendanceRecords
            .Where(r => employeeIds.Contains(r.EmployeeId) && r.Date >= startDate && r.Date <= endDate)
            .ToListAsync();

        var totalEmployees = employeeIds.Count;
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

        return new AttendanceKpiDto
        {
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
            TotalLostMinutes = totalLostMinutes,
            TotalLostHours = Math.Round(totalLostMinutes / 60.0, 2)
        };
    }

    private async Task<List<CriticalAlertDto>> GetCriticalAlertsAsync()
    {
        var alerts = new List<CriticalAlertDto>();
        var today = DateTime.Today;

        // High absenteeism departments (>10% absent)
        var departments = await _context.Departments.ToListAsync();
        foreach (var dept in departments)
        {
            var deptEmployees = await _context.Employees
                .Where(e => e.DepartmentId == dept.Id && e.IsActive)
                .Select(e => e.EmployeeId)
                .ToListAsync();

            if (!deptEmployees.Any()) continue;

            var todayRecords = await _context.AttendanceRecords
                .Where(r => deptEmployees.Contains(r.EmployeeId) && r.Date == today)
                .ToListAsync();

            var presentCount = todayRecords.Count(r => r.ClockInTime.HasValue);
            var absenteeismRate = deptEmployees.Count > 0
                ? Math.Round((double)(deptEmployees.Count - presentCount) / deptEmployees.Count * 100, 2)
                : 0;

            if (absenteeismRate > 10)
            {
                alerts.Add(new CriticalAlertDto
                {
                    AlertType = "HighAbsenteeism",
                    Severity = "High",
                    DepartmentId = dept.Id,
                    DepartmentName = dept.Name,
                    Message = $"Department {dept.Name} has {absenteeismRate}% absenteeism today",
                    Value = absenteeismRate,
                    Threshold = 10,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // Overdue investigations (>48 hours)
        var overdueInvestigations = await _context.AttendanceExceptions
            .Include(e => e.Employee)
                .ThenInclude(e => e!.Department)
            .Where(e => (e.InvestigationStatus == InvestigationStatus.Open || e.InvestigationStatus == InvestigationStatus.UnderReview) &&
                       e.CreatedAt < DateTime.UtcNow.AddHours(-48))
            .ToListAsync();

        foreach (var exc in overdueInvestigations)
        {
            var hoursOverdue = (DateTime.UtcNow - exc.CreatedAt).TotalHours;
            alerts.Add(new CriticalAlertDto
            {
                AlertType = "OverdueInvestigation",
                Severity = hoursOverdue > 72 ? "Critical" : "High",
                DepartmentId = exc.Employee?.DepartmentId,
                DepartmentName = exc.Employee?.Department?.Name,
                Message = $"Exception for {exc.Employee?.DisplayName} is {hoursOverdue:F0} hours overdue for investigation",
                Value = hoursOverdue,
                Threshold = 48,
                RelatedExceptionId = exc.Id,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Pending escalations (>24 hours)
        var pendingEscalations = await _context.Escalations
            .Include(e => e.Employee)
                .ThenInclude(e => e!.Department)
            .Where(e => (e.Status == EscalationStatus.Pending || e.Status == EscalationStatus.Accepted) &&
                       e.CreatedAt < DateTime.UtcNow.AddHours(-24))
            .ToListAsync();

        foreach (var esc in pendingEscalations)
        {
            var hoursPending = (DateTime.UtcNow - esc.CreatedAt).TotalHours;
            alerts.Add(new CriticalAlertDto
            {
                AlertType = "PendingEscalation",
                Severity = hoursPending > 48 ? "Critical" : "High",
                DepartmentId = esc.Employee?.DepartmentId,
                DepartmentName = esc.Employee?.Department?.Name,
                Message = $"Escalation Level {esc.Level} for {esc.Employee?.DisplayName} pending for {hoursPending:F0} hours",
                Value = hoursPending,
                Threshold = 24,
                RelatedEscalationId = esc.Id,
                CreatedAt = DateTime.UtcNow
            });
        }

        return alerts.OrderByDescending(a => a.Severity).ThenByDescending(a => a.Value).ToList();
    }
}

// DTOs
public record ExecutiveDashboardDto
{
    public DateTime Date { get; init; }
    public AttendanceKpiDto TodaySnapshot { get; init; } = null!;
    public AttendanceKpiDto WeekToDateSnapshot { get; init; } = null!;
    public AttendanceKpiDto MonthToDateSnapshot { get; init; } = null!;
    public List<CriticalAlertDto> CriticalAlerts { get; init; } = new();
    public List<DepartmentKpiDto> TopPerformingDepartments { get; init; } = new();
    public List<DepartmentKpiDto> BottomPerformingDepartments { get; init; } = new();
    public int TotalDepartments { get; init; }
}

public record ManagerDashboardDto
{
    public Guid DepartmentId { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public AttendanceKpiDto MonthToDateKpi { get; init; } = null!;
    public int TotalEmployees { get; init; }
    public List<EmployeeSummaryDto> Employees { get; init; } = new();
    public List<ExceptionSummaryDto> RecentExceptions { get; init; } = new();
    public int OpenInvestigations { get; init; }
    public int PendingEscalations { get; init; }
}

public record SupervisorDashboardDto
{
    public string SupervisorEmployeeId { get; init; } = string.Empty;
    public string SupervisorName { get; init; } = string.Empty;
    public List<string> DirectReports { get; init; } = new();
    public AttendanceKpiDto? WeekToDateKpi { get; init; }
    public List<TodayAttendanceDto> TodayAttendance { get; init; } = new();
    public List<ExceptionSummaryDto> PendingInvestigations { get; init; } = new();
    public List<EscalationSummaryDto> PendingEscalations { get; init; } = new();
}

public record EmployeeDashboardDto
{
    public string EmployeeId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public double MonthToDateAttendanceRate { get; init; }
    public int MonthToDatePresentDays { get; init; }
    public int MonthToDateLateDays { get; init; }
    public int MonthToDateEarlyDepartureDays { get; init; }
    public int MonthToDateTotalLateMinutes { get; init; }
    public int MonthToDateTotalEarlyMinutes { get; init; }
    public TodayAttendanceDto? TodayRecord { get; init; }
    public List<ExceptionSummaryDto> RecentExceptions { get; init; } = new();
    public List<WarningSummaryDto> Warnings { get; init; } = new();
}

public record EmployeeSummaryDto
{
    public string EmployeeId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string JobTitle { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

public record ExceptionSummaryDto
{
    public Guid Id { get; init; }
    public string EmployeeId { get; init; } = string.Empty;
    public string EmployeeName { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public string ExceptionType { get; init; } = string.Empty;
    public InvestigationStatus Status { get; init; }
    public int LostMinutes { get; init; }
}

public record EscalationSummaryDto
{
    public Guid Id { get; init; }
    public string EmployeeId { get; init; } = string.Empty;
    public string EmployeeName { get; init; } = string.Empty;
    public int Level { get; init; }
    public EscalationStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public string ActionRequired { get; init; } = string.Empty;
}

public record TodayAttendanceDto
{
    public string EmployeeId { get; init; } = string.Empty;
    public string EmployeeName { get; init; } = string.Empty;
    public DateTime? ClockIn { get; init; }
    public DateTime? ClockOut { get; init; }
    public bool IsLate { get; init; }
    public bool IsEarlyDeparture { get; init; }
    public int MinutesLate { get; init; }
    public int MinutesEarly { get; init; }
    public AttendanceStatus Status { get; init; }
}

public record WarningSummaryDto
{
    public Guid Id { get; init; }
    public WarningType Type { get; init; }
    public DateTime DateIssued { get; init; }
    public string Reason { get; init; } = string.Empty;
    public bool Signed { get; init; }
}

public record CriticalAlertDto
{
    public string AlertType { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public Guid? DepartmentId { get; init; }
    public string? DepartmentName { get; init; }
    public string Message { get; init; } = string.Empty;
    public double Value { get; init; }
    public double Threshold { get; init; }
    public Guid? RelatedExceptionId { get; init; }
    public Guid? RelatedEscalationId { get; init; }
    public DateTime CreatedAt { get; init; }
}
