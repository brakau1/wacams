using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WACAMS.Api.Services;

namespace WACAMS.Api.Controllers;

/// <summary>
/// Audit trail endpoints — get audit logs with filters and pagination.
/// All changes to the system are recorded here for compliance and accountability.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuditController : ControllerBase
{
    private readonly AuditService _auditService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(AuditService auditService, ILogger<AuditController> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/audit — Get audit trail with filtering and pagination.
    /// Query params: actorId, action, entityType, entityId, startDate, endDate, page, pageSize.
    /// Admin/SuperAdmin only.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof((List<AuditLogDto>, int)), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] string? actorId,
        [FromQuery] string? action,
        [FromQuery] string? entityType,
        [FromQuery] string? entityId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var (logs, totalCount) = await _auditService.GetTrailAsync(
            actorId, action, entityType, entityId, startDate, endDate, page, pageSize);
        return Ok(new { logs, totalCount, page, pageSize });
    }

    /// <summary>
    /// GET /api/audit/recent — Get recent activity feed (last N actions).
    /// Used for dashboard activity displays.
    /// Any authenticated user can access this.
    /// Query params: limit (default 20, max 100).
    /// </summary>
    [HttpGet("recent")]
    [Authorize]
    [ProducesResponseType(typeof(List<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecent([FromQuery] int limit = 20)
    {
        if (limit > 100)
            limit = 100; // Hard cap

        var logs = await _auditService.GetRecentActivityAsync(limit);
        return Ok(logs);
    }
}
