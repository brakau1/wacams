using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WACAMS.Api.DTOs;
using WACAMS.Api.Services;
using WACAMS.Api.Middleware;
using ErrorResponse = WACAMS.Api.DTOs.ErrorResponse;

namespace WACAMS.Api.Controllers;

/// <summary>
/// Attendance exception CRUD — investigate workflow, bulk investigate.
/// Status flow: Pending → Investigated → Closed
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExceptionsController : ControllerBase
{
    private readonly ExceptionService _exceptionService;
    private readonly ILogger<ExceptionsController> _logger;

    public ExceptionsController(ExceptionService exceptionService, ILogger<ExceptionsController> logger)
    {
        _exceptionService = exceptionService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/exceptions — List exceptions with filters and pagination.
    /// Query params: employeeId, startDate, endDate, exceptionType, status, departmentId, page, pageSize.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof((List<AttendanceExceptionDto>, int)), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] string? employeeId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? exceptionType,
        [FromQuery] string? status,
        [FromQuery] int? departmentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var (exceptions, totalCount) = await _exceptionService.ListAsync(
            employeeId, startDate, endDate, exceptionType, status, departmentId, page, pageSize);
        return Ok(new { exceptions, totalCount, page, pageSize });
    }

    /// <summary>
    /// POST /api/exceptions — Create a new attendance exception manually.
    /// Admin/Supervisor/HR only.
    /// Valid types: LateArrival, EarlyDeparture, UnplannedAbsenteeism, Awol, SickLeave,
    /// FamilyResponsibilityLeave, ShiftCancellation, WorkshopAttendance, Training,
    /// MissingClockIn, MissingClockOut, UnscheduledClockIn, UnscheduledClockOut.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin,Supervisor,HR")]
    [ProducesResponseType(typeof(AttendanceExceptionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateExceptionRequest request)
    {
        try
        {
            var result = await _exceptionService.CreateAsync(request);
            return CreatedAtAction(nameof(Get), new { }, result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = "NotFound", message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { error = "Duplicate", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "InvalidInput", message = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/exceptions/{id}/investigate — Investigate an exception.
    /// Transitions from Pending → Investigated.
    /// Supervisor, Admin, SuperAdmin, HR only.
    /// </summary>
    [HttpPut("{id:int}/investigate")]
    [Authorize(Roles = "SuperAdmin,Admin,Supervisor,HR")]
    [ProducesResponseType(typeof(AttendanceExceptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Investigate(int id, [FromBody] InvestigateExceptionRequest request)
    {
        var investigatedBy = User.FindFirst("employeeId")?.Value
            ?? throw new InvalidOperationException("No employeeId in claims.");

        try
        {
            var result = await _exceptionService.InvestigateAsync(id, request, investigatedBy);
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = "NotFound", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = "InvalidState", message = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/exceptions/bulk-investigate — Bulk investigate multiple exceptions at once.
    /// Supervisor, Admin, SuperAdmin, HR only.
    /// </summary>
    [HttpPut("bulk-investigate")]
    [Authorize(Roles = "SuperAdmin,Admin,Supervisor,HR")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkInvestigate([FromBody] BulkInvestigateRequest request)
    {
        var investigatedBy = User.FindFirst("employeeId")?.Value
            ?? throw new InvalidOperationException("No employeeId in claims.");

        var count = await _exceptionService.BulkInvestigateAsync(request.ExceptionIds, new InvestigateExceptionRequest { InvestigationNotes = request.Notes }, investigatedBy);
        return Ok(new { count });
    }

    /// <summary>
    /// PUT /api/exceptions/{id}/close — Close an investigated exception.
    /// Transitions from Investigated → Closed.
    /// Admin, Supervisor, SuperAdmin, HR only.
    /// </summary>
    [HttpPut("{id:int}/close")]
    [Authorize(Roles = "SuperAdmin,Admin,Supervisor,HR")]
    [ProducesResponseType(typeof(AttendanceExceptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Close(int id)
    {
        var closedBy = User.FindFirst("employeeId")?.Value
            ?? throw new InvalidOperationException("No employeeId in claims.");

        try
        {
            var result = await _exceptionService.CloseAsync(id, closedBy);
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = "NotFound", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = "InvalidState", message = ex.Message });
        }
    }
}

public record BulkInvestigateRequest(List<Guid> ExceptionIds, string Notes = "");
