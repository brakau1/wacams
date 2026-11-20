using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WACAMS.Api.DTOs.GIS;
using WACAMS.Infrastructure.Services.GIS;

namespace WACAMS.Api.Controllers.GIS;

/// <summary>
/// GIS iWork Integration Controller
/// Provides REST API endpoints for GIS data synchronization and exception detection
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GISIntegrationController : ControllerBase
{
    private readonly IGISIntegrationService _gisService;
    private readonly ILogger<GISIntegrationController> _logger;

    public GISIntegrationController(
        IGISIntegrationService gisService,
        ILogger<GISIntegrationController> logger)
    {
        _gisService = gisService;
        _logger = logger;
    }

    #region Real-Time Sync Endpoints

    /// <summary>
    /// Sync employee data in real-time from GIS iWork
    /// </summary>
    [HttpPost("sync/employee")]
    [ProducesResponseType(typeof(GISSyncResultDTO), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SyncEmployeeRealTime([FromBody] GISEmployeeDTO employee, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(employee.EmployeeId))
        {
            return BadRequest("Employee ID is required");
        }

        _logger.LogInformation("Real-time employee sync requested for {EmployeeId}", employee.EmployeeId);
        var result = await _gisService.SyncEmployeeRealTimeAsync(employee, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Sync clock transaction in real-time from GIS iWork
    /// </summary>
    [HttpPost("sync/clock-transaction")]
    [ProducesResponseType(typeof(GISSyncResultDTO), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SyncClockTransactionRealTime([FromBody] GISClockTransactionDTO transaction, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(transaction.EmployeeId))
        {
            return BadRequest("Employee ID is required");
        }

        _logger.LogInformation("Real-time clock transaction sync requested for {EmployeeId}", transaction.EmployeeId);
        var result = await _gisService.SyncClockTransactionRealTimeAsync(transaction, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Sync attendance record in real-time from GIS iWork
    /// </summary>
    [HttpPost("sync/attendance")]
    [ProducesResponseType(typeof(GISSyncResultDTO), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SyncAttendanceRecordRealTime([FromBody] GISAttendanceRecordDTO record, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(record.EmployeeId))
        {
            return BadRequest("Employee ID is required");
        }

        _logger.LogInformation("Real-time attendance sync requested for {EmployeeId}", record.EmployeeId);
        var result = await _gisService.SyncAttendanceRecordRealTimeAsync(record, cancellationToken);
        return Ok(result);
    }

    #endregion

    #region Scheduled Sync Endpoints

    /// <summary>
    /// Trigger scheduled sync for all employees
    /// </summary>
    [HttpPost("sync/employees/scheduled")]
    [Authorize(Roles = "Administrator,ProductionManager")]
    [ProducesResponseType(typeof(GISSyncResultDTO), 200)]
    public async Task<IActionResult> SyncAllEmployeesScheduled(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Scheduled employee sync triggered");
        var result = await _gisService.SyncAllEmployeesAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Trigger scheduled sync for attendance records
    /// </summary>
    [HttpPost("sync/attendance/scheduled")]
    [Authorize(Roles = "Administrator,ProductionManager")]
    [ProducesResponseType(typeof(GISSyncResultDTO), 200)]
    public async Task<IActionResult> SyncAttendanceRecordsScheduled(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Scheduled attendance sync triggered for {From} to {To}", fromDate, toDate);
        var result = await _gisService.SyncAttendanceRecordsAsync(fromDate, toDate, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Trigger scheduled sync for shift schedules
    /// </summary>
    [HttpPost("sync/shift-schedules/scheduled")]
    [Authorize(Roles = "Administrator,ProductionManager")]
    [ProducesResponseType(typeof(GISSyncResultDTO), 200)]
    public async Task<IActionResult> SyncShiftSchedulesScheduled(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Scheduled shift schedule sync triggered for {From} to {To}", fromDate, toDate);
        var result = await _gisService.SyncShiftSchedulesAsync(fromDate, toDate, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Trigger scheduled sync for leave records
    /// </summary>
    [HttpPost("sync/leave-records/scheduled")]
    [Authorize(Roles = "Administrator,ProductionManager")]
    [ProducesResponseType(typeof(GISSyncResultDTO), 200)]
    public async Task<IActionResult> SyncLeaveRecordsScheduled(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Scheduled leave record sync triggered for {From} to {To}", fromDate, toDate);
        var result = await _gisService.SyncLeaveRecordsAsync(fromDate, toDate, cancellationToken);
        return Ok(result);
    }

    #endregion

    #region Exception Detection Endpoints

    /// <summary>
    /// Detect all attendance exceptions for a specific date
    /// </summary>
    [HttpGet("exceptions/detect")]
    [ProducesResponseType(typeof(List<GISExceptionDetectionResultDTO>), 200)]
    public async Task<IActionResult> DetectExceptions([FromQuery] DateTime date, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Exception detection requested for date {Date}", date);
        var exceptions = await _gisService.DetectExceptionsAsync(date, cancellationToken);
        return Ok(exceptions);
    }

    /// <summary>
    /// Detect late arrivals for a specific date
    /// </summary>
    [HttpGet("exceptions/late-arrivals")]
    [ProducesResponseType(typeof(List<GISExceptionDetectionResultDTO>), 200)]
    public async Task<IActionResult> DetectLateArrivals([FromQuery] DateTime date, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Late arrival detection requested for date {Date}", date);
        var exceptions = await _gisService.DetectLateArrivalsAsync(date, cancellationToken);
        return Ok(exceptions);
    }

    /// <summary>
    /// Detect early departures for a specific date
    /// </summary>
    [HttpGet("exceptions/early-departures")]
    [ProducesResponseType(typeof(List<GISExceptionDetectionResultDTO>), 200)]
    public async Task<IActionResult> DetectEarlyDepartures([FromQuery] DateTime date, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Early departure detection requested for date {Date}", date);
        var exceptions = await _gisService.DetectEarlyDeparturesAsync(date, cancellationToken);
        return Ok(exceptions);
    }

    /// <summary>
    /// Detect unplanned absences for a specific date
    /// </summary>
    [HttpGet("exceptions/unplanned-absences")]
    [ProducesResponseType(typeof(List<GISExceptionDetectionResultDTO>), 200)]
    public async Task<IActionResult> DetectUnplannedAbsences([FromQuery] DateTime date, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unplanned absence detection requested for date {Date}", date);
        var exceptions = await _gisService.DetectUnplannedAbsencesAsync(date, cancellationToken);
        return Ok(exceptions);
    }

    /// <summary>
    /// Detect missing clock transactions for a specific date
    /// </summary>
    [HttpGet("exceptions/missing-transactions")]
    [ProducesResponseType(typeof(List<GISExceptionDetectionResultDTO>), 200)]
    public async Task<IActionResult> DetectMissingTransactions([FromQuery] DateTime date, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Missing transaction detection requested for date {Date}", date);
        var exceptions = await _gisService.DetectMissingTransactionsAsync(date, cancellationToken);
        return Ok(exceptions);
    }

    #endregion

    #region Query Endpoints

    /// <summary>
    /// Get employee details from GIS
    /// </summary>
    [HttpGet("employees/{employeeId}")]
    [ProducesResponseType(typeof(GISEmployeeDTO), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetEmployee(string employeeId, CancellationToken cancellationToken)
    {
        var employee = await _gisService.GetEmployeeAsync(employeeId, cancellationToken);
        if (employee == null)
        {
            return NotFound($"Employee {employeeId} not found");
        }
        return Ok(employee);
    }

    /// <summary>
    /// Get attendance records for an employee
    /// </summary>
    [HttpGet("attendance/{employeeId}")]
    [ProducesResponseType(typeof(List<GISAttendanceRecordDTO>), 200)]
    public async Task<IActionResult> GetAttendanceRecords(
        string employeeId,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken)
    {
        var records = await _gisService.GetAttendanceRecordsAsync(employeeId, fromDate, toDate, cancellationToken);
        return Ok(records);
    }

    /// <summary>
    /// Get last successful sync time for a specific sync type
    /// </summary>
    [HttpGet("sync/last-sync/{syncType}")]
    [ProducesResponseType(typeof(DateTime?), 200)]
    public async Task<IActionResult> GetLastSyncTime(string syncType, CancellationToken cancellationToken)
    {
        var lastSync = await _gisService.GetLastSyncTimeAsync(syncType, cancellationToken);
        return Ok(lastSync);
    }

    #endregion
}
