using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WACAMS.Api.DTOs;
using WACAMS.Api.Services;
using WACAMS.Core.Exceptions;
using ErrorResponse = WACAMS.Api.DTOs.ErrorResponse;

namespace WACAMS.Api.Controllers;

/// <summary>
/// Report endpoints — attendance summary, department summary, individual report, export CSV/PDF.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly ReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(ReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/reports/department-summary — Get attendance summary for all departments over a date range.
    /// Calculates Department Attendance %, Employee Attendance %, Lost Production Hours.
    /// Query params: startDate, endDate.
    /// </summary>
    [HttpGet("department-summary")]
    [Authorize]
    [ProducesResponseType(typeof(List<DTOs.DepartmentAttendanceSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDepartmentSummary(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        if (startDate > endDate)
            return BadRequest(new { error = "InvalidRange", message = "startDate must be before endDate." });

        var summaries = await _reportService.GetDepartmentSummariesAsync(startDate, endDate);
        return Ok(summaries);
    }

    /// <summary>
    /// GET /api/reports/individual/{employeeId} — Individual employee attendance report with full details.
    /// Query params: startDate, endDate.
    /// </summary>
    [HttpGet("individual/{employeeId}")]
    [Authorize]
    [ProducesResponseType(typeof(DTOs.IndividualAttendanceReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIndividual(string employeeId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            var report = await _reportService.GetIndividualReportAsync(employeeId, startDate, endDate);
            return Ok(report);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = "NotFound", message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/reports/export-csv — Export attendance data to CSV format.
    /// Query params: startDate, endDate, departmentId (optional).
    /// Returns a CSV file download.
    /// </summary>
    [HttpGet("export-csv")]
    [Authorize]
    [Produces("text/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? departmentId)
    {
        if (startDate > endDate)
            return BadRequest(new { error = "InvalidRange", message = "startDate must be before endDate." });

        var csvContent = await _reportService.ExportAttendanceCsvAsync(startDate, endDate, departmentId);
        var fileName = $"attendance_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv";

        return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "text/csv", fileName);
    }

    /// <summary>
    /// GET /api/reports/warnings-export-csv — Export warnings to CSV format.
    /// Query params: employeeId, startDate, endDate (all optional).
    /// </summary>
    [HttpGet("warnings-export-csv")]
    [Authorize]
    [Produces("text/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportWarningsCsv(
        [FromQuery] string? employeeId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var csvContent = await _reportService.ExportWarningsCsvAsync(employeeId, startDate, endDate);
        var fileName = $"warnings_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";

        return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "text/csv", fileName);
    }

    /// <summary>
    /// GET /api/reports/exception-breakdown — Count exceptions by type for dashboard KPIs.
    /// Query params: startDate, endDate, departmentId (optional).
    /// </summary>
    [HttpGet("exception-breakdown")]
    [Authorize]
    [ProducesResponseType(typeof(Dictionary<string, int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExceptionBreakdown(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int? departmentId)
    {
        if (startDate > endDate)
            return BadRequest(new { error = "InvalidRange", message = "startDate must be before endDate." });

        var breakdown = await _reportService.GetExceptionBreakdownAsync(startDate, endDate, departmentId);
        return Ok(breakdown);
    }

    /// <summary>
    /// GET /api/reports/progressive-discipline/{employeeId} — Progressive discipline summary.
    /// Shows how many of each warning type an employee has on file.
    /// </summary>
    [HttpGet("progressive-discipline/{employeeId}")]
    [Authorize]
    [ProducesResponseType(typeof(ProgressiveDisciplineSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProgressiveDiscipline(string employeeId)
    {
        try
        {
            var summary = await _reportService.GetProgressiveDisciplineSummaryAsync(employeeId);
            return Ok(summary);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = "NotFound", message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/reports/dashboard-kpi — General dashboard KPIs for authenticated users.
    /// Returns department breakdown, trends, top late arrivals, and recent exceptions.
    /// </summary>
    [HttpGet("dashboard-kpi")]
    [Authorize]
    [ProducesResponseType(typeof(DashboardKPIResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardKPI()
    {
        var today = DateTime.UtcNow.Date;
        var last14Days = today.AddDays(-13);

        // Get all departments with today's stats
        var departments = await _reportService.GetDepartmentKPIsAsync(today);

        // Get 14-day trend
        var trend = await _reportService.GetTrendAsync(last14Days, today);

        // Get top late arrivals (last 30 days)
        var topLate = await _reportService.GetTopLateArrivalsAsync(today.AddDays(-29), today, 10);

        // Get recent exceptions (last 7 days)
        var recentExceptions = await _reportService.GetRecentExceptionsAsync(today.AddDays(-6), today, 20);

        // Calculate summary KPIs
        var totalEmployees = departments.Sum(d => d.TotalEmployees);
        var totalPresent = departments.Sum(d => d.PresentCount);
        var totalLate = departments.Sum(d => d.LateCount);
        var avgOnTime = departments.Any() ? Math.Round(departments.Average(d => d.AttendancePercentage), 1) : 0;
        var avgLateMinutes = topLate.Any() ? (int)Math.Round(topLate.Average(t => t.TotalLateMinutes)) : 0;
        var activeExceptions = recentExceptions.Count(e => e.Status == "Open" || e.Status == "UnderReview");

        var response = new DashboardKPIResponseDto
        {
            KPI = new DashboardSummaryDto
            {
                TotalEmployees = totalEmployees,
                OnTimePercentage = avgOnTime,
                AverageLateMinutes = avgLateMinutes,
                ActiveExceptions = activeExceptions,
                PendingWarnings = 0 // TODO: Calculate from warnings service
            },
            Departments = departments,
            Trends = trend,
            TopLateArrivals = topLate,
            RecentExceptions = recentExceptions
        };

        return Ok(response);
    }

    /// <summary>
    /// GET /api/reports/manager-dashboard — Manager dashboard with department-specific view.
    /// Returns summary, team breakdown, trends, top offenders, and exceptions.
    /// Query params: departmentId (optional, defaults to user's department).
    /// </summary>
    [HttpGet("manager-dashboard")]
    [Authorize]
    [ProducesResponseType(typeof(ManagerDashboardResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetManagerDashboard([FromQuery] Guid? departmentId)
    {
        var today = DateTime.UtcNow.Date;
        var last14Days = today.AddDays(-13);
        var last30Days = today.AddDays(-29);

        // If no departmentId provided, use first department (TODO: get from user claims)
        var targetDeptId = departmentId ?? Guid.Empty;

        // Get department summary
        var summary = await _reportService.GetDepartmentSummaryAsync(today, targetDeptId);

        // Get team summary (sub-departments if any)
        var teamSummary = await _reportService.GetTeamSummaryAsync(today, targetDeptId);

        // Get all departments for comparison
        var departments = await _reportService.GetDepartmentKPIsAsync(today);

        // Get 14-day trend for this department
        var trend = await _reportService.GetTrendAsync(last14Days, today, targetDeptId);

        // Get top offenders (last 30 days)
        var topOffenders = await _reportService.GetTopLateArrivalsAsync(last30Days, today, 10, targetDeptId);

        // Get recent exceptions (last 7 days)
        var exceptions = await _reportService.GetRecentExceptionsAsync(today.AddDays(-6), today, 50, targetDeptId);

        var response = new ManagerDashboardResponseDto
        {
            Summary = summary,
            TeamSummary = teamSummary,
            Departments = departments,
            Trends = trend,
            TopOffenders = topOffenders,
            Exceptions = exceptions
        };

        return Ok(response);
    }

    /// <summary>
    /// GET /api/reports/hr-command-centre — HR Command Centre with plant-wide oversight.
    /// Returns KPIs, trends, department rankings, top offenders, and exception analysis.
    /// </summary>
    [HttpGet("hr-command-centre")]
    [Authorize]
    [ProducesResponseType(typeof(HRCommandCentreResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHRCommandCentre()
    {
        var today = DateTime.UtcNow.Date;
        var last14Days = today.AddDays(-13);
        var last30Days = today.AddDays(-29);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        // Get plant-wide KPIs
        var kpis = await _reportService.GetHRCommandCentreKPIsAsync(today, monthStart);

        // Get 14-day trend
        var trend = await _reportService.GetTrendAsync(last14Days, today);

        // Get department rankings with risk levels
        var rankings = await _reportService.GetDepartmentRankingsAsync(today);

        // Get top offenders (last 30 days)
        var topOffenders = await _reportService.GetTopLateArrivalsAsync(last30Days, today, 10);

        // Get exception analysis by type
        var exceptionAnalysis = await _reportService.GetExceptionAnalysisAsync(monthStart, today);

        var response = new HRCommandCentreResponseDto
        {
            KPIs = kpis,
            AttendanceTrend = trend,
            DepartmentRanking = rankings,
            TopOffenders = topOffenders,
            ExceptionAnalysis = exceptionAnalysis
        };

        return Ok(response);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// DASHBOARD DTOs — Phase 3 Live Dashboard Integration
// ═══════════════════════════════════════════════════════════════════════════════

public class DashboardKPIResponseDto
{
    public DashboardSummaryDto KPI { get; set; } = new();
    public List<DashboardDepartmentDto> Departments { get; set; } = new();
    public List<DashboardTrendDto> Trends { get; set; } = new();
    public List<TopLateArrivalDto> TopLateArrivals { get; set; } = new();
    public List<DashboardExceptionDto> RecentExceptions { get; set; } = new();
}

public class DashboardSummaryDto
{
    public int TotalEmployees { get; set; }
    public double OnTimePercentage { get; set; }
    public int AverageLateMinutes { get; set; }
    public int ActiveExceptions { get; set; }
    public int PendingWarnings { get; set; }
}

public class DashboardDepartmentDto
{
    public string DepartmentId { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public double AttendancePercentage { get; set; }
    public int TotalEmployees { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
}

public class DashboardTrendDto
{
    public string Date { get; set; } = "";
    public double OnTimePercentage { get; set; }
    public int LateCount { get; set; }
    public int ExceptionCount { get; set; }
}

public class TopLateArrivalDto
{
    public string EmployeeId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public int LateCount { get; set; }
    public int TotalLateMinutes { get; set; }
}

public class DashboardExceptionDto
{
    public string Id { get; set; } = "";
    public string EmployeeId { get; set; } = "";
    public string EmployeeName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string DepartmentId { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public string ExceptionType { get; set; } = "";
    public string Date { get; set; } = "";
    public string Description { get; set; } = "";
    public int LostProductionMinutes { get; set; }
    public string Status { get; set; } = "";
    public string? AssignedTo { get; set; }
    public string? AssignedToName { get; set; }
    public string? InvestigatorNotes { get; set; }
    public DateTime? InvestigatedAt { get; set; }
    public string? InvestigatedBy { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}

public class ManagerDashboardResponseDto
{
    public ManagerSummaryDto Summary { get; set; } = new();
    public ManagerSummaryDto TeamSummary { get; set; } = new();
    public List<DashboardDepartmentDto> Departments { get; set; } = new();
    public List<DashboardTrendDto> Trends { get; set; } = new();
    public List<TopLateArrivalDto> TopOffenders { get; set; } = new();
    public List<DashboardExceptionDto> Exceptions { get; set; } = new();
}

public class ManagerSummaryDto
{
    public string DepartmentId { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public int TotalEmployees { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public int AwolCount { get; set; }
    public int SickLeaveCount { get; set; }
    public int FamilyResponsibilityCount { get; set; }
    public int AnnualLeaveCount { get; set; }
    public double AttendancePercentage { get; set; }
    public int TotalLateMinutes { get; set; }
    public int TotalEarlyMinutes { get; set; }
    public int TotalLostMinutes { get; set; }
}

public class HRCommandCentreResponseDto
{
    public HRCommandCentreKPIsDto KPIs { get; set; } = new();
    public List<DashboardTrendDto> AttendanceTrend { get; set; } = new();
    public List<DepartmentRankingDto> DepartmentRanking { get; set; } = new();
    public List<TopLateArrivalDto> TopOffenders { get; set; } = new();
    public List<ExceptionAnalysisDto> ExceptionAnalysis { get; set; } = new();
}

public class HRCommandCentreKPIsDto
{
    public double PlantAttendancePercentage { get; set; }
    public int LostProductionHours { get; set; }
    public int OpenWarnings { get; set; }
    public int FinalWarnings { get; set; }
    public int AwolCases { get; set; }
    public int Escalations { get; set; }
    public int DepartmentsAtRisk { get; set; }
}

public class DepartmentRankingDto
{
    public string DepartmentId { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public double AttendancePercentage { get; set; }
    public int TotalEmployees { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public int ExceptionCount { get; set; }
    public string RiskLevel { get; set; } = "low";
}

public class ExceptionAnalysisDto
{
    public string ExceptionType { get; set; } = "";
    public int Count { get; set; }
    public double Percentage { get; set; }
    public int LostMinutes { get; set; }
}
