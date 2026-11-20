using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wacam.Api.DTOs;
using Wacam.Api.Services;
using WACAMS.Core.Entities;

namespace Wacam.Api.Controllers;

/// <summary>
/// SuperAdmin API endpoints for user management, role management, access control, and employee overrides.
/// All operations require SuperAdmin role and are audit logged.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Policy = "RequireSuperAdmin")]
public class SuperAdminController : ControllerBase
{
    private readonly SuperAdminService _superAdminService;
    private readonly ILogger<SuperAdminController> _logger;

    public SuperAdminController(
        SuperAdminService superAdminService,
        ILogger<SuperAdminController> logger)
    {
        _superAdminService = superAdminService;
        _logger = logger;
    }

    private string GetCurrentEmployeeId()
    {
        return User.FindFirst("employeeId")?.Value ?? "system";
    }

    #region User Management

    /// <summary>
    /// GET /api/admin/users - Get all users with pagination and filtering
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<object>> GetAllUsers(
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var (users, totalCount) = await _superAdminService.GetAllUsersAsync(search, role, isActive, page, pageSize);
        return Ok(new { users, totalCount, page, pageSize });
    }

    /// <summary>
    /// GET /api/admin/users/{employeeId} - Get user by EmployeeId
    /// </summary>
    [HttpGet("users/{employeeId}")]
    public async Task<ActionResult<UserManagementDto>> GetUserByEmployeeId(string employeeId)
    {
        var user = await _superAdminService.GetUserByEmployeeIdAsync(employeeId);
        if (user == null)
        {
            return NotFound(new { message = $"User {employeeId} not found" });
        }
        return Ok(user);
    }

    /// <summary>
    /// POST /api/admin/users - Register a new user
    /// </summary>
    [HttpPost("users")]
    public async Task<ActionResult<UserManagementDto>> RegisterUser([FromBody] RegisterUserRequest request)
    {
        try
        {
            var adminEmployeeId = GetCurrentEmployeeId();
            var user = await _superAdminService.RegisterUserAsync(request, adminEmployeeId);
            return CreatedAtAction(nameof(GetUserByEmployeeId), new { employeeId = user.EmployeeId }, user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/admin/users/{employeeId} - Update user details
    /// </summary>
    [HttpPut("users/{employeeId}")]
    public async Task<ActionResult<UserManagementDto>> UpdateUser(string employeeId, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var adminEmployeeId = GetCurrentEmployeeId();
            var user = await _superAdminService.UpdateUserAsync(employeeId, request, adminEmployeeId);
            return Ok(user);
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
    /// POST /api/admin/users/{employeeId}/reset-password - Reset user password
    /// </summary>
    [HttpPost("users/{employeeId}/reset-password")]
    public async Task<IActionResult> ResetPassword(string employeeId, [FromBody] ResetPasswordRequest request)
    {
        try
        {
            var adminEmployeeId = GetCurrentEmployeeId();
            await _superAdminService.ResetPasswordAsync(employeeId, request, adminEmployeeId);
            return Ok(new { message = "Password reset successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/admin/users/{employeeId}/lock - Lock user account
    /// </summary>
    [HttpPost("users/{employeeId}/lock")]
    public async Task<IActionResult> LockAccount(string employeeId, [FromBody] LockAccountRequest? request = null)
    {
        try
        {
            var adminEmployeeId = GetCurrentEmployeeId();
            await _superAdminService.LockAccountAsync(employeeId, request?.Reason, adminEmployeeId);
            return Ok(new { message = "Account locked successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/admin/users/{employeeId}/unlock - Unlock user account
    /// </summary>
    [HttpPost("users/{employeeId}/unlock")]
    public async Task<IActionResult> UnlockAccount(string employeeId)
    {
        try
        {
            var adminEmployeeId = GetCurrentEmployeeId();
            await _superAdminService.UnlockAccountAsync(employeeId, adminEmployeeId);
            return Ok(new { message = "Account unlocked successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/admin/users/{employeeId}/login-history - Get user login history
    /// </summary>
    [HttpGet("users/{employeeId}/login-history")]
    public async Task<ActionResult<List<UserLoginHistoryDto>>> GetUserLoginHistory(string employeeId, [FromQuery] int limit = 100)
    {
        var history = await _superAdminService.GetUserLoginHistoryAsync(employeeId, limit);
        return Ok(history);
    }

    #endregion

    #region Role Management

    /// <summary>
    /// GET /api/admin/roles - Get all roles with user counts
    /// </summary>
    [HttpGet("roles")]
    public async Task<ActionResult<List<RoleDto>>> GetAllRoles()
    {
        var roles = await _superAdminService.GetAllRolesAsync();
        return Ok(roles);
    }

    /// <summary>
    /// POST /api/admin/roles/assign - Assign role to user
    /// </summary>
    [HttpPost("roles/assign")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
    {
        try
        {
            var adminEmployeeId = GetCurrentEmployeeId();
            await _superAdminService.AssignRoleAsync(request, adminEmployeeId);
            return Ok(new { message = $"Role {request.Role} assigned to user {request.EmployeeId}" });
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
    /// POST /api/admin/roles/remove/{employeeId} - Remove role from user (set to User role)
    /// </summary>
    [HttpPost("roles/remove/{employeeId}")]
    public async Task<IActionResult> RemoveRole(string employeeId, [FromBody] RemoveRoleRequest? request = null)
    {
        try
        {
            var adminEmployeeId = GetCurrentEmployeeId();
            await _superAdminService.RemoveRoleAsync(employeeId, request?.Reason, adminEmployeeId);
            return Ok(new { message = $"Role removed from user {employeeId}" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    #endregion

    #region Access Control

    /// <summary>
    /// GET /api/admin/access-matrix - Get access matrix for all roles
    /// </summary>
    [HttpGet("access-matrix")]
    public async Task<ActionResult<List<AccessMatrixDto>>> GetAccessMatrix()
    {
        var matrix = await _superAdminService.GetAccessMatrixAsync();
        return Ok(matrix);
    }

    /// <summary>
    /// PUT /api/admin/access-matrix - Update access matrix for a role and module
    /// </summary>
    [HttpPut("access-matrix")]
    public async Task<IActionResult> UpdateAccessMatrix([FromBody] UpdateAccessMatrixRequest request)
    {
        try
        {
            var adminEmployeeId = GetCurrentEmployeeId();
            await _superAdminService.UpdateAccessMatrixAsync(request, adminEmployeeId);
            return Ok(new { message = $"Access matrix updated for role {request.Role}, module {request.Module}" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    #region Employee Access Overrides

    /// <summary>
    /// GET /api/admin/overrides - Get all employee access overrides
    /// </summary>
    [HttpGet("overrides")]
    public async Task<ActionResult<List<EmployeeOverrideDto>>> GetAllOverrides()
    {
        var overrides = await _superAdminService.GetAllOverridesAsync();
        return Ok(overrides);
    }

    /// <summary>
    /// GET /api/admin/overrides/{id} - Get override by ID
    /// </summary>
    [HttpGet("overrides/{id}")]
    public async Task<ActionResult<EmployeeOverrideDto>> GetOverrideById(Guid id)
    {
        var overrideEntity = await _superAdminService.GetOverrideByIdAsync(id);
        if (overrideEntity == null)
        {
            return NotFound(new { message = $"Override {id} not found" });
        }
        return Ok(overrideEntity);
    }

    /// <summary>
    /// POST /api/admin/overrides - Create employee access override
    /// </summary>
    [HttpPost("overrides")]
    public async Task<ActionResult<EmployeeOverrideDto>> CreateOverride([FromBody] CreateEmployeeOverrideRequest request)
    {
        try
        {
            var adminEmployeeId = GetCurrentEmployeeId();
            var overrideEntity = await _superAdminService.CreateOverrideAsync(request, adminEmployeeId);
            return CreatedAtAction(nameof(GetOverrideById), new { id = overrideEntity.Id }, overrideEntity);
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
    /// PUT /api/admin/overrides/{id} - Update employee access override
    /// </summary>
    [HttpPut("overrides/{id}")]
    public async Task<ActionResult<EmployeeOverrideDto>> UpdateOverride(Guid id, [FromBody] UpdateEmployeeOverrideRequest request)
    {
        try
        {
            var adminEmployeeId = GetCurrentEmployeeId();
            var overrideEntity = await _superAdminService.UpdateOverrideAsync(id, request, adminEmployeeId);
            return Ok(overrideEntity);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// DELETE /api/admin/overrides/{id} - Delete employee access override
    /// </summary>
    [HttpDelete("overrides/{id}")]
    public async Task<IActionResult> DeleteOverride(Guid id)
    {
        try
        {
            var adminEmployeeId = GetCurrentEmployeeId();
            await _superAdminService.DeleteOverrideAsync(id, adminEmployeeId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    #endregion

    #region SuperAdmin Creation Workflow

    /// <summary>
    /// POST /api/admin/superadmin/create - Create a new SuperAdmin (only protected SuperAdmin can do this)
    /// </summary>
    [HttpPost("superadmin/create")]
    public async Task<ActionResult<UserManagementDto>> CreateSuperAdmin([FromBody] SuperAdminCreationRequest request)
    {
        try
        {
            var requestingEmployeeId = GetCurrentEmployeeId();
            var user = await _superAdminService.CreateSuperAdminAsync(request, requestingEmployeeId);
            return CreatedAtAction(nameof(GetUserByEmployeeId), new { employeeId = user.EmployeeId }, user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/admin/superadmin/protection - Get SuperAdmin protection status
    /// </summary>
    [HttpGet("superadmin/protection")]
    public async Task<ActionResult<SuperAdminProtectionDto>> GetProtectionStatus()
    {
        var status = await _superAdminService.GetProtectionStatusAsync();
        return Ok(status);
    }

    #endregion

    #region Security Audit Logs

    /// <summary>
    /// GET /api/admin/security-audit - Get security audit logs
    /// </summary>
    [HttpGet("security-audit")]
    public async Task<ActionResult<object>> GetSecurityAuditLogs([FromQuery] SecurityAuditQuery query)
    {
        var (logs, totalCount) = await _superAdminService.GetSecurityAuditLogsAsync(query);
        return Ok(new { logs, totalCount, page = query.Page, pageSize = query.PageSize });
    }

    #endregion
}
