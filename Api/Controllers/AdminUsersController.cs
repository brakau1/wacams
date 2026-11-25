using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WACAMS.Api.DTOs;
using WACAMS.Core.Entities;
using WACAMS.Api.Services;

namespace WACAMS.Api.Controllers;

/// <summary>
/// API endpoints for managing admin users who have full system access regardless of their role.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly AdminUserService _adminUserService;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(AdminUserService adminUserService, ILogger<AdminUsersController> logger)
    {
        _adminUserService = adminUserService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/adminusers - Get all admin users
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AdminUser>>> GetAllAdmins()
    {
        var admins = await _adminUserService.GetAllAdminsAsync();
        return Ok(admins);
    }

    /// <summary>
    /// GET /api/adminusers/{email} - Get admin user details
    /// </summary>
    [HttpGet("{email}")]
    public async Task<ActionResult<AdminUser>> GetAdminByEmail(string email)
    {
        var admin = await _adminUserService.GetAdminByEmailAsync(email);
        if (admin == null)
        {
            return NotFound(new { message = $"Admin user {email} not found" });
        }
        return Ok(admin);
    }

    /// <summary>
    /// POST /api/adminusers - Add a user to the admin list
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AdminUser>> AddAdmin([FromBody] AddAdminRequest request)
    {
        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var admin = await _adminUserService.AddAdminAsync(
                request.Email, 
                request.DisplayName, 
                request.Reason, 
                currentUser);
            
            return CreatedAtAction(nameof(GetAdminByEmail), new { email = admin.Email }, admin);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// DELETE /api/adminusers/{email} - Remove a user from the admin list
    /// </summary>
    [HttpDelete("{email}")]
    public async Task<IActionResult> RemoveAdmin(string email)
    {
        var currentUser = User.Identity?.Name ?? "Unknown";
        var success = await _adminUserService.RemoveAdminAsync(email, currentUser);
        
        if (!success)
        {
            return NotFound(new { message = $"Admin user {email} not found" });
        }
        
        return NoContent();
    }

    /// <summary>
    /// GET /api/adminusers/check/{email} - Check if a user has admin access
    /// </summary>
    [HttpGet("check/{email}")]
    [AllowAnonymous]
    public async Task<ActionResult<bool>> CheckIsAdmin(string email)
    {
        var isAdmin = await _adminUserService.IsAdminAsync(email);
        return Ok(new { email, isAdmin });
    }
}
