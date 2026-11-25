using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WACAMS.Api.DTOs;
using WACAMS.Api.Services;

namespace WACAMS.Api.Controllers;

/// <summary>
/// SuperAdmin API endpoints for security analytics and metrics.
/// All operations require SuperAdmin role and are audit logged.
/// </summary>
[ApiController]
[Route("api/admin/security-analytics")]
[Authorize(Policy = "RequireSuperAdmin")]
public class SecurityAnalyticsController : ControllerBase
{
    private readonly SecurityAnalyticsService _analyticsService;
    private readonly ILogger<SecurityAnalyticsController> _logger;

    public SecurityAnalyticsController(
        SecurityAnalyticsService analyticsService,
        ILogger<SecurityAnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/admin/security-analytics/overview - Get comprehensive security analytics
    /// </summary>
    [HttpGet("overview")]
    public async Task<ActionResult<SecurityAnalyticsDto>> GetAnalytics()
    {
        try
        {
            var analytics = await _analyticsService.GetAnalyticsAsync();
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving security analytics");
            return StatusCode(500, new { message = "Error retrieving security analytics", error = ex.Message });
        }
    }
}
