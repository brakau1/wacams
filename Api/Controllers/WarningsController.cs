using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WACAMS.Api.DTOs;
using WACAMS.Api.Middleware;
using WACAMS.Api.Services;
using WACAMS.Core.Exceptions;

namespace WACAMS.Api.Controllers;

/// <summary>
/// Warning letter endpoints — generate warnings (post-investigation), upload signed forms, list by employee/dept.
/// Progressive discipline: Counselling → VerbalWarning → WrittenWarning → FinalWrittenWarning
/// Guard: MUST verify Exception status is "Investigated" or "Closed" before generating any warning.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class WarningsController : ControllerBase
{
    private readonly WarningService _warningService;
    private readonly ILogger<WarningsController> _logger;

    public WarningsController(WarningService warningService, ILogger<WarningsController> logger)
    {
        _warningService = warningService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/warnings — List warnings with filters and pagination.
    /// Query params: employeeId, departmentId, warningType, startDate, endDate, acknowledged, page, pageSize.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof((List<WarningLetterDto>, int)), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] string? employeeId,
        [FromQuery] int? departmentId,
        [FromQuery] string? warningType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] bool? acknowledged,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var (warnings, totalCount) = await _warningService.ListAsync(
            employeeId, departmentId, warningType, startDate, endDate, acknowledged, page, pageSize);
        return Ok(new { warnings, totalCount, page, pageSize });
    }

    /// <summary>
    /// POST /api/warnings/generate — Generate a warning letter from an attendedce exception.
    /// REQUIRES: Exception must be in "Investigated" or "Closed" status.
    /// Supervisor, Admin, SuperAdmin, HR only.
    /// Auto-triggers escalation when FinalWrittenWarning is generated.
    /// </summary>
    [HttpPost("generate")]
    [Authorize(Roles = "SuperAdmin,Admin,Supervisor,HR")]
    [ProducesResponseType(typeof(WarningLetterDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Generate([FromBody] GenerateWarningRequest request)
    {
        var issuedBy = User.FindFirst("employeeId")?.Value
            ?? throw new InvalidOperationException("No employeeId in claims.");

        try
        {
            var result = await _warningService.GenerateWarningAsync(request, issuedBy);
            return CreatedAtAction(nameof(Get), new { }, result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = "NotFound", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = "InvalidState", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "InvalidInput", message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/warnings/{id}/upload-signed — Upload a signed warning form PDF.
    /// Multipart/form-data file upload to wwwroot/signatures/.
    /// </summary>
    [HttpPost("{id:guid}/upload-signed")]
    [Authorize]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)] // Returns relative path
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadSignedForm(Guid id)
    {
        var file = Request.Form.Files["file"];

        try
        {
            var path = await _warningService.UploadSignedFormAsync(id, file);
            return Ok(new { signedFormPath = path });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = "NotFound", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "InvalidFile", message = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/warnings/{id}/acknowledge — Mark a warning as acknowledged by the employee.
    /// </summary>
    [HttpPut("{id:guid}/acknowledge")]
    [Authorize]
    [ProducesResponseType(typeof(WarningLetterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Acknowledge(Guid id)
    {
        var employeeId = User.FindFirst("employeeId")?.Value
            ?? throw new InvalidOperationException("No employeeId in claims.");

        try
        {
            var result = await _warningService.AcknowledgeAsync(id, employeeId);
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = "NotFound", message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/warnings/{employeeId}/counts — Get progressive discipline counts for an employee.
    /// Shows how many of each warning type they have on file.
    /// </summary>
    [HttpGet("{employeeId}/counts")]
    [Authorize]
    [ProducesResponseType(typeof(Dictionary<string, int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWarningCounts(string employeeId)
    {
        var counts = await _warningService.GetWarningCountsAsync(employeeId);
        return Ok(counts);
    }
}
