using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WACAMS.Api.DTOs;
using WACAMS.Api.Services;

namespace WACAMS.Api.Controllers;

/// <summary>
/// SuperAdmin API endpoints for access review and snapshot management.
/// All operations require SuperAdmin role and are audit logged.
/// </summary>
[ApiController]
[Route("api/admin/access-review")]
[Authorize(Policy = "RequireSuperAdmin")]
public class AccessReviewController : ControllerBase
{
    private readonly AccessReviewService _accessReviewService;
    private readonly ILogger<AccessReviewController> _logger;

    public AccessReviewController(
        AccessReviewService accessReviewService,
        ILogger<AccessReviewController> logger)
    {
        _accessReviewService = accessReviewService;
        _logger = logger;
    }

    private string GetCurrentEmployeeId()
    {
        return User.FindFirst("employeeId")?.Value ?? "system";
    }

    /// <summary>
    /// GET /api/admin/access-review/explorer - Get all user access explorer data
    /// </summary>
    [HttpGet("explorer")]
    public async Task<ActionResult<List<UserAccessExplorerDto>>> GetAllUserAccess()
    {
        try
        {
            var accessData = await _accessReviewService.GetAllUserAccessAsync();
            return Ok(accessData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user access data");
            return StatusCode(500, new { message = "Error retrieving user access data", error = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/admin/access-review/explorer/{employeeId} - Get user access explorer data
    /// </summary>
    [HttpGet("explorer/{employeeId}")]
    public async Task<ActionResult<UserAccessExplorerDto>> GetUserAccess(string employeeId)
    {
        try
        {
            var accessData = await _accessReviewService.GetUserAccessAsync(employeeId);
            if (accessData == null)
            {
                return NotFound(new { message = $"User {employeeId} not found" });
            }
            return Ok(accessData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user access data for {EmployeeId}", employeeId);
            return StatusCode(500, new { message = "Error retrieving user access data", error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/admin/access-review/snapshot - Capture access snapshot for a user
    /// </summary>
    [HttpPost("snapshot")]
    public async Task<ActionResult<AccessReviewSnapshotDto>> CaptureSnapshot([FromBody] CaptureSnapshotRequest request)
    {
        try
        {
            var capturedBy = GetCurrentEmployeeId();
            var snapshot = await _accessReviewService.CaptureSnapshotAsync(request.EmployeeId, capturedBy, request.Reason);
            return CreatedAtAction(nameof(GetSnapshots), new { employeeId = request.EmployeeId }, snapshot);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing access snapshot for {EmployeeId}", request.EmployeeId);
            return StatusCode(500, new { message = "Error capturing access snapshot", error = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/admin/access-review/snapshots/{employeeId} - Get access review snapshots for a user
    /// </summary>
    [HttpGet("snapshots/{employeeId}")]
    public async Task<ActionResult<List<AccessReviewSnapshotDto>>> GetSnapshots(string employeeId, [FromQuery] int limit = 50)
    {
        try
        {
            var snapshots = await _accessReviewService.GetSnapshotsAsync(employeeId, limit);
            return Ok(snapshots);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving access snapshots for {EmployeeId}", employeeId);
            return StatusCode(500, new { message = "Error retrieving access snapshots", error = ex.Message });
        }
    }
}
