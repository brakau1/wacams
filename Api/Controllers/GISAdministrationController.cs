using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wacam.Api.Services;
using Wacam.Api.DTOs;

namespace Wacam.Api.Controllers;

/// <summary>
/// SuperAdmin API endpoints for GIS integration administration.
/// All operations require SuperAdmin role and are audit logged.
/// </summary>
[ApiController]
[Route("api/admin/gis")]
[Authorize(Policy = "RequireSuperAdmin")]
public class GISAdministrationController : ControllerBase
{
    private readonly GISAdminService _gisAdminService;
    private readonly ILogger<GISAdministrationController> _logger;

    public GISAdministrationController(
        GISAdminService gisAdminService,
        ILogger<GISAdministrationController> logger)
    {
        _gisAdminService = gisAdminService;
        _logger = logger;
    }

    private string GetCurrentEmployeeId()
    {
        return User.FindFirst("employeeId")?.Value ?? "system";
    }

    /// <summary>
    /// GET /api/admin/gis/connection-status - Get GIS connection status
    /// </summary>
    [HttpGet("connection-status")]
    public async Task<ActionResult<GisConnectionStatusDto>> GetConnectionStatus()
    {
        try
        {
            var status = await _gisAdminService.GetConnectionStatusAsync();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GIS connection status");
            return StatusCode(500, new { message = "Error retrieving GIS connection status", error = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/admin/gis/sync-history - Get GIS sync history
    /// </summary>
    [HttpGet("sync-history")]
    public async Task<ActionResult<List<GisSyncHistoryDto>>> GetSyncHistory([FromQuery] int limit = 50)
    {
        try
        {
            var history = await _gisAdminService.GetSyncHistoryAsync(limit);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving GIS sync history");
            return StatusCode(500, new { message = "Error retrieving GIS sync history", error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/admin/gis/sync/manual - Trigger manual GIS sync
    /// </summary>
    [HttpPost("sync/manual")]
    public async Task<ActionResult<GisSyncHistoryDto>> TriggerManualSync([FromBody] ManualSyncRequest request)
    {
        try
        {
            var initiatedBy = GetCurrentEmployeeId();
            var result = await _gisAdminService.TriggerManualSyncAsync(request.SyncType, request.FromDate, request.ToDate, initiatedBy);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering manual GIS sync");
            return StatusCode(500, new { message = "Error triggering manual GIS sync", error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/admin/gis/sync/pause - Pause GIS sync
    /// </summary>
    [HttpPost("sync/pause")]
    public async Task<IActionResult> PauseSync()
    {
        try
        {
            var initiatedBy = GetCurrentEmployeeId();
            await _gisAdminService.PauseSyncAsync(initiatedBy);
            return Ok(new { message = "GIS sync paused successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing GIS sync");
            return StatusCode(500, new { message = "Error pausing GIS sync", error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/admin/gis/sync/resume - Resume GIS sync
    /// </summary>
    [HttpPost("sync/resume")]
    public async Task<IActionResult> ResumeSync()
    {
        try
        {
            var initiatedBy = GetCurrentEmployeeId();
            await _gisAdminService.ResumeSyncAsync(initiatedBy);
            return Ok(new { message = "GIS sync resumed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming GIS sync");
            return StatusCode(500, new { message = "Error resuming GIS sync", error = ex.Message });
        }
    }
}
