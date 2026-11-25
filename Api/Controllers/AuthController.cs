using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WACAMS.Api.DTOs;
using WACAMS.Api.Services;

namespace WACAMS.Api.Controllers;

/// <summary>
/// Authentication endpoints — login, register, refresh-token, logout.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/auth/login — Authenticate and receive JWT access + refresh tokens.
    /// Enforces account lockout after 5 failed attempts within 30 minutes.
    /// Returns 423 Locked if account is locked.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status423Locked)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        try
        {
            var result = await _authService.LoginAsync(request, ipAddress);
            return Ok(result);
        }
        catch (AccountLockedException ex)
        {
            return StatusCode(StatusCodes.Status423Locked, new { error = "AccountLocked", message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = "InvalidCredentials", message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/auth/register — Register a new employee (Admin/SuperAdmin only).
    /// </summary>
    [HttpPost("register")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);
            return CreatedAtAction(nameof(Register), new { id = result.Id }, result);
        }
        catch (ConflictException ex)
        {
            return Conflict(new { error = "DuplicateEmployee", message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/auth/refresh — Refresh an expired access token using a valid refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var principal = TokenGenerator.ValidateToken(request.AccessToken);
            if (principal is null)
                throw new UnauthorizedAccessException("Invalid access token.");

            var employeeId = TokenGenerator.GetEmployeeIdFromToken(principal);
            var result = await _authService.RefreshTokenAsync(employeeId, request.RefreshToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = "InvalidToken", message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/auth/logout — Invalidate the current refresh token.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        var employeeId = User.FindFirst("employeeId")?.Value
            ?? throw new InvalidOperationException("No employeeId in claims.");

        await _authService.LogoutAsync(employeeId);
        return Ok(new { message = "Logged out successfully." });
    }

    /// <summary>
    /// PUT /api/auth/change-password — Change the authenticated user's password.
    /// </summary>
    [HttpPut("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var employeeId = User.FindFirst("employeeId")?.Value
            ?? throw new InvalidOperationException("No employeeId in claims.");

        try
        {
            await _authService.ChangePasswordAsync(employeeId, request);
            return Ok(new { message = "Password changed successfully." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "ValidationFailed", message = ex.Message });
        }
    }
}
