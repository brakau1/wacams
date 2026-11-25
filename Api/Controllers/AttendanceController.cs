using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WACAMS.Api.DTOs;
using WACAMS.Api.Middleware;
using WACAMS.Api.Services;

namespace WACAMS.Api.Controllers;

/// <summary>
/// Attendance endpoints — clock in/out, daily/weekly/monthly stats, exception reporting.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AttendanceController : ControllerBase
{
    private readonly AttendanceService _attendanceService;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(AttendanceService attendanceService, ILogger<AttendanceController> logger)
    {
        _attendanceService = attendanceService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/attendance/clockin — Clock in for an employee.
    /// Automatically calculates lateness based on shift time.
    /// Creates LateArrival exception if >30 min late (after grace period).
    /// </summary>
    [HttpPost("clockin")]
    [Authorize]
    [ProducesResponseType(typeof(AttendanceRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ClockIn([FromBody] ClockInRequest request)
    {
        try
        {
            var result = await _attendanceService.ClockInAsync(request);
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = "NotFound", message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { error = "AlreadyClockedIn", message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/attendance/clockout — Clock out for an employee.
    /// Automatically calculates early departure based on shift end time.
    /// </summary>
    [HttpPost("clockout")]
    [Authorize]
    [ProducesResponseType(typeof(AttendanceRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ClockOut([FromBody] ClockOutRequest request)
    {
        try
        {
            var result = await _attendanceService.ClockOutAsync(request);
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = "NotFound", message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { error = "AlreadyClockedOut", message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/attendance/daily — Get daily attendance records for all employees or by department.
    /// Query params: date (required), departmentId (optional).
    /// </summary>
    [HttpGet("daily")]
    [Authorize]
    [ProducesResponseType(typeof(List<AttendanceRecordDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDaily(
        [FromQuery] DateTime date,
        [FromQuery] Guid? departmentId)
    {
        var records = await _attendanceService.GetDailyAsync(date, departmentId);
        return Ok(records);
    }

    /// <summary>
    /// GET /api/attendance/summary — Get aggregated attendance summary for a date range.
    /// Calculates Department Attendance %, Employee Attendance %, Lost Production Hours.
    /// Query params: startDate, endDate, departmentId (optional).
    /// </summary>
    [HttpGet("summary")]
    [Authorize]
    [ProducesResponseType(typeof(AttendanceSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] Guid? departmentId)
    {
        if (startDate > endDate)
            return BadRequest(new { error = "InvalidRange", message = "startDate must be before endDate." });

        var summary = await _attendanceService.GetSummaryAsync(startDate, endDate, departmentId);
        return Ok(summary);
    }

    /// <summary>
    /// GET /api/attendance/individual/{employeeId} — Individual employee attendance report.
    /// Query params: startDate, endDate.
    /// </summary>
    [HttpGet("individual/{employeeId}")]
    [Authorize]
    [ProducesResponseType(typeof(IndividualAttendanceReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIndividual(string employeeId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            var report = await _attendanceService.GetIndividualReportAsync(employeeId, startDate, endDate);
            return Ok(report);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = "NotFound", message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/attendance/scan-missing-clockins — Scan for missing clock-ins (admin/system endpoint).
    /// Creates UnplannedAbsenteeism exceptions for employees without records past threshold.
    /// </summary>
    [HttpPost("scan-missing-clockins")]
    [Authorize(Roles = "SuperAdmin,Admin,Supervisor")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ScanMissingClockIns([FromQuery] DateTime date)
    {
        await _attendanceService.ScanMissingClockInsAsync(date);
        return Ok(new { message = $"Scan completed for {date:yyyy-MM-dd}." });
    }

    /// <summary>
    /// POST /api/attendance/scan-missing-clockouts — Scan for missing clock-outs.
    /// Creates MissingClockOut exceptions for employees who haven't clocked out after shift end.
    /// </summary>
    [HttpPost("scan-missing-clockouts")]
    [Authorize(Roles = "SuperAdmin,Admin,Supervisor")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ScanMissingClockOuts([FromQuery] DateTime date)
    {
        await _attendanceService.ScanMissingClockOutsAsync(date);
        return Ok(new { message = $"Scan completed for {date:yyyy-MM-dd}." });
    }
}
