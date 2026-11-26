using Microsoft.AspNetCore.Authorization;
using WACAMS.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using WACAMS.Api.DTOs;
using WACAMS.Api.Services;
using WACAMS.Api.Middleware;
using ErrorResponse = WACAMS.Api.DTOs.ErrorResponse;

namespace WACAMS.Api.Controllers;

/// <summary>
/// Escalation endpoints — list, progress escalation, get chain.
/// Auto-created when FinalWrittenWarning is generated.
/// Follows supervisor hierarchy via ManagerId chain.
/// Each manager has 24 hours to act before auto-escalating.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EscalationsController : ControllerBase
{
    private readonly EscalationService _escalationService;
    private readonly ILogger<EscalationsController> _logger;

    public EscalationsController(EscalationService escalationService, ILogger<EscalationsController> logger)
    {
        _escalationService = escalationService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/escalations — List escalations with filters and pagination.
    /// Query params: employeeId, status, level, departmentId, page, pageSize.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof((List<EscalationDto>, int)), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] string? employeeId,
        [FromQuery] string? status,
        [FromQuery] int? level,
        [FromQuery] Guid? departmentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var (escalations, totalCount) = await _escalationService.ListAsync(
            employeeId, status, level, departmentId, page, pageSize);
        return Ok(new { escalations, totalCount, page, pageSize });
    }

    /// <summary>
    /// PUT /api/escalations/{id}/progress — Progress an escalation.
    /// Acknowledge at current level or escalate to next level in the chain.
    /// Responsible manager can mark as acknowledged; if overdue, auto-escalates.
    /// </summary>
    [HttpPut("{id:guid}/progress")]
    [Authorize]
    [ProducesResponseType(typeof(EscalationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Progress(Guid id, [FromBody] ProgressEscalationRequest request)
    {
        var actedBy = User.FindFirst("employeeId")?.Value
            ?? throw new InvalidOperationException("No employeeId in claims.");

        try
        {
            var result = await _escalationService.ProgressEscalationAsync(id, request, actedBy);
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
    /// GET /api/escalations/{employeeId}/chain — Get the full escalation chain for an employee.
    /// Shows all management levels from direct supervisor up to top-level manager.
    /// </summary>
    [HttpGet("{employeeId}/chain")]
    [Authorize]
    [ProducesResponseType(typeof(EscalationChainResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChain(string employeeId)
    {
        try
        {
            var chain = await _escalationService.GetEscalationChainAsync(employeeId);
            return Ok(chain);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = "NotFound", message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/escalations/scan-overdue — Scan for overdue escalations (admin/system endpoint).
    /// Auto-escalates any pending escalation where the manager hasn't acted within 24 hours.
    /// Should be called by a scheduled background job.
    /// </summary>
    [HttpPost("scan-overdue")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ScanOverdue()
    {
        await _escalationService.ScanOverdueEscalationsAsync();
        return Ok(new { message = "Overdue escalation scan completed." });
    }
}
