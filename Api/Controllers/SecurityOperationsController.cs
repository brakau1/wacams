using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WACAMS.Api.DTOs;
using WACAMS.Api.Services;

namespace WACAMS.Api.Controllers;

/// <summary>
/// SuperAdmin API endpoints for emergency security operations.
/// All operations require SuperAdmin role and are audit logged.
/// </summary>
[ApiController]
[Route("api/admin/security-operations")]
[Authorize(Policy = "RequireSuperAdmin")]
public class SecurityOperationsController : ControllerBase
{
    private readonly SecurityOperationsService _securityOpsService;
    private readonly ILogger<SecurityOperationsController> _logger;

    public SecurityOperationsController(
        SecurityOperationsService securityOpsService,
        ILogger<SecurityOperationsController> logger)
    {
        _securityOpsService = securityOpsService;
        _logger = logger;
    }

    private string GetCurrentEmployeeId()
    {
        return User.FindFirst("employeeId")?.Value ?? "system";
    }

    /// <summary>
    /// POST /api/admin/security-operations/force-logout - Force logout a user
    /// </summary>
    [HttpPost("force-logout")]
    public async Task<IActionResult> ForceLogout([FromBody] ForceLogoutRequest request)
    {
        try
        {
            var initiatedBy = GetCurrentEmployeeId();
            var success = await _securityOpsService.ForceLogoutAsync(request.EmployeeId, request.Reason, initiatedBy);
            if (!success)
            {
                return NotFound(new { message = $"User {request.EmployeeId} not found" });
            }
            return Ok(new { message = "User force logged out successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/admin/security-operations/lock-user - Lock a user account immediately
    /// </summary>
    [HttpPost("lock-user")]
    public async Task<IActionResult> LockUser([FromBody] ForceLogoutRequest request)
    {
        try
        {
            var initiatedBy = GetCurrentEmployeeId();
            var success = await _securityOpsService.LockUserAsync(request.EmployeeId, request.Reason, initiatedBy);
            if (!success)
            {
                return NotFound(new { message = $"User {request.EmployeeId} not found" });
            }
            return Ok(new { message = "User account locked successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/admin/security-operations/unlock-user - Unlock a user account
    /// </summary>
    [HttpPost("unlock-user")]
    public async Task<IActionResult> UnlockUser([FromBody] ForceLogoutRequest request)
    {
        try
        {
            var initiatedBy = GetCurrentEmployeeId();
            var success = await _securityOpsService.UnlockUserAsync(request.EmployeeId, initiatedBy);
            if (!success)
            {
                return NotFound(new { message = $"User {request.EmployeeId} not found" });
            }
            return Ok(new { message = "User account unlocked successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/admin/security-operations/disable-user - Disable a user account
    /// </summary>
    [HttpPost("disable-user")]
    public async Task<IActionResult> DisableUser([FromBody] ForceLogoutRequest request)
    {
        try
        {
            var initiatedBy = GetCurrentEmployeeId();
            var success = await _securityOpsService.DisableUserAsync(request.EmployeeId, request.Reason, initiatedBy);
            if (!success)
            {
                return NotFound(new { message = $"User {request.EmployeeId} not found" });
            }
            return Ok(new { message = "User account disabled successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/admin/security-operations/enable-user - Enable a user account
    /// </summary>
    [HttpPost("enable-user")]
    public async Task<IActionResult> EnableUser([FromBody] ForceLogoutRequest request)
    {
        try
        {
            var initiatedBy = GetCurrentEmployeeId();
            var success = await _securityOpsService.EnableUserAsync(request.EmployeeId, initiatedBy);
            if (!success)
            {
                return NotFound(new { message = $"User {request.EmployeeId} not found" });
            }
            return Ok(new { message = "User account enabled successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/admin/security-operations/revoke-sessions - Revoke all active sessions for a user
    /// </summary>
    [HttpPost("revoke-sessions")]
    public async Task<IActionResult> RevokeSessions([FromBody] ForceLogoutRequest request)
    {
        try
        {
            var initiatedBy = GetCurrentEmployeeId();
            var count = await _securityOpsService.RevokeAllSessionsAsync(request.EmployeeId, request.Reason, initiatedBy);
            return Ok(new { message = $"Revoked {count} active sessions", count });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/admin/security-operations/force-password-reset - Force password reset for a user
    /// </summary>
    [HttpPost("force-password-reset")]
    public async Task<IActionResult> ForcePasswordReset([FromBody] ForceLogoutRequest request)
    {
        try
        {
            var initiatedBy = GetCurrentEmployeeId();
            var success = await _securityOpsService.ForcePasswordResetAsync(request.EmployeeId, initiatedBy);
            if (!success)
            {
                return NotFound(new { message = $"User {request.EmployeeId} not found" });
            }
            return Ok(new { message = "Password reset forced successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/admin/security-operations/initiate-lockdown - Initiate a security lockdown
    /// </summary>
    [HttpPost("initiate-lockdown")]
    public async Task<ActionResult<SecurityLockdownDto>> InitiateLockdown([FromBody] InitiateLockdownRequest request)
    {
        try
        {
            var initiatedBy = GetCurrentEmployeeId();
            var lockdown = await _securityOpsService.InitiateLockdownAsync(request, initiatedBy);
            return CreatedAtAction(nameof(GetActiveLockdowns), new { }, lockdown);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/admin/security-operations/end-lockdown/{id} - End a security lockdown
    /// </summary>
    [HttpPost("end-lockdown/{id}")]
    public async Task<IActionResult> EndLockdown(Guid id)
    {
        try
        {
            var endedBy = GetCurrentEmployeeId();
            var success = await _securityOpsService.EndLockdownAsync(id, endedBy);
            if (!success)
            {
                return NotFound(new { message = $"Lockdown {id} not found" });
            }
            return Ok(new { message = "Security lockdown ended successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/admin/security-operations/events - Get security events with filtering
    /// </summary>
    [HttpGet("events")]
    public async Task<ActionResult<object>> GetSecurityEvents([FromQuery] SecurityEventQuery query)
    {
        var (events, totalCount) = await _securityOpsService.GetSecurityEventsAsync(query);
        return Ok(new { events, totalCount, page = query.Page, pageSize = query.PageSize });
    }

    /// <summary>
    /// POST /api/admin/security-operations/events/{id}/acknowledge - Acknowledge a security event
    /// </summary>
    [HttpPost("events/{id}/acknowledge")]
    public async Task<IActionResult> AcknowledgeSecurityEvent(Guid id, [FromBody] AcknowledgeSecurityEventRequest request)
    {
        try
        {
            var acknowledgedBy = GetCurrentEmployeeId();
            var success = await _securityOpsService.AcknowledgeSecurityEventAsync(id, request.Notes, acknowledgedBy);
            if (!success)
            {
                return NotFound(new { message = $"Security event {id} not found" });
            }
            return Ok(new { message = "Security event acknowledged successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/admin/security-operations/lockdowns - Get all active security lockdowns
    /// </summary>
    [HttpGet("lockdowns")]
    public async Task<ActionResult<List<SecurityLockdownDto>>> GetActiveLockdowns()
    {
        var lockdowns = await _securityOpsService.GetActiveLockdownsAsync();
        return Ok(lockdowns);
    }

    /// <summary>
    /// GET /api/admin/security-operations/sessions - Get all active user sessions
    /// </summary>
    [HttpGet("sessions")]
    public async Task<ActionResult<List<UserSessionDto>>> GetActiveSessions()
    {
        var sessions = await _securityOpsService.GetActiveSessionsAsync();
        return Ok(sessions);
    }

    /// <summary>
    /// GET /api/admin/security-operations/sessions/{employeeId} - Get active sessions for a specific user
    /// </summary>
    [HttpGet("sessions/{employeeId}")]
    public async Task<ActionResult<List<UserSessionDto>>> GetUserSessions(string employeeId)
    {
        var sessions = await _securityOpsService.GetUserSessionsAsync(employeeId);
        return Ok(sessions);
    }
}
