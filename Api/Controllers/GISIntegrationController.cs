using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WACAMS.Api.DTOs;
using WACAMS.Api.Services;
using WACAMS.Core.Entities;

namespace WACAMS.Api.Controllers;

/// <summary>
/// GIS Integration API endpoints.
/// Admin/SuperAdmin only - manages GIS connection settings, authentication, and testing.
/// </summary>
[ApiController]
[Route("api/gis-integration")]
[Produces("application/json")]
public class GISIntegrationController : ControllerBase
{
    private readonly GISAuthenticationService _authService;
    private readonly EncryptionService _encryptionService;
    private readonly AuditService _auditService;
    private readonly ILogger<GISIntegrationController> _logger;

    public GISIntegrationController(
        GISAuthenticationService authService,
        EncryptionService encryptionService,
        AuditService auditService,
        ILogger<GISIntegrationController> logger)
    {
        _authService = authService;
        _encryptionService = encryptionService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/gis-integration/settings - Get current GIS integration settings.
    /// </summary>
    [HttpGet("settings")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(GISIntegrationSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _authService.GetSettingsAsync();

        if (settings == null)
        {
            return Ok(new GISIntegrationSettingsDto
            {
                IsEnabled = false,
                SyncIntervalMinutes = 60,
                UsePlaywrightAuth = true,
                PlaywrightTimeoutSeconds = 30,
                MaxRetryAttempts = 3
            });
        }

        return Ok(new GISIntegrationSettingsDto
        {
            Id = settings.Id,
            PortalBaseUrl = settings.PortalBaseUrl,
            IWorkApiBaseUrl = settings.IWorkApiBaseUrl,
            SsoEndpoint = settings.SsoEndpoint,
            MaskedUsername = _encryptionService.Mask(_encryptionService.Decrypt(settings.EncryptedUsername)),
            DepartmentId = settings.DepartmentId,
            DepartmentName = settings.DepartmentName,
            IsEnabled = settings.IsEnabled,
            SyncIntervalMinutes = settings.SyncIntervalMinutes,
            LastConnectionTestAt = settings.LastConnectionTestAt,
            LastConnectionStatus = settings.LastConnectionStatus,
            LastConnectionError = settings.LastConnectionError,
            UsePlaywrightAuth = settings.UsePlaywrightAuth,
            PlaywrightTimeoutSeconds = settings.PlaywrightTimeoutSeconds,
            MaxRetryAttempts = settings.MaxRetryAttempts,
            UpdatedAt = settings.UpdatedAt,
            UpdatedBy = settings.UpdatedBy
        });
    }

    /// <summary>
    /// PUT /api/gis-integration/settings - Create or update GIS integration settings.
    /// </summary>
    [HttpPut("settings")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(GISIntegrationSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateGISIntegrationSettingsRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "ValidationError",
                Message = "Invalid request data",
                Details = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
            });
        }

        var currentUser = User.FindFirst("employeeId")?.Value ?? "unknown";

        var settings = new GISIntegrationSettings
        {
            PortalBaseUrl = request.PortalBaseUrl.TrimEnd('/'),
            IWorkApiBaseUrl = request.IWorkApiBaseUrl.TrimEnd('/'),
            SsoEndpoint = request.SsoEndpoint,
            DepartmentId = request.DepartmentId,
            DepartmentName = request.DepartmentName,
            IsEnabled = request.IsEnabled,
            SyncIntervalMinutes = request.SyncIntervalMinutes,
            UsePlaywrightAuth = request.UsePlaywrightAuth,
            PlaywrightTimeoutSeconds = request.PlaywrightTimeoutSeconds,
            MaxRetryAttempts = request.MaxRetryAttempts
        };

        var updated = await _authService.UpdateSettingsAsync(settings, request.Username, request.Password, currentUser);

        _logger.LogInformation("GIS integration settings updated by {User}", currentUser);

        return Ok(new GISIntegrationSettingsDto
        {
            Id = updated.Id,
            PortalBaseUrl = updated.PortalBaseUrl,
            IWorkApiBaseUrl = updated.IWorkApiBaseUrl,
            SsoEndpoint = updated.SsoEndpoint,
            MaskedUsername = _encryptionService.Mask(_encryptionService.Decrypt(updated.EncryptedUsername)),
            DepartmentId = updated.DepartmentId,
            DepartmentName = updated.DepartmentName,
            IsEnabled = updated.IsEnabled,
            SyncIntervalMinutes = updated.SyncIntervalMinutes,
            LastConnectionTestAt = updated.LastConnectionTestAt,
            LastConnectionStatus = updated.LastConnectionStatus,
            LastConnectionError = updated.LastConnectionError,
            UsePlaywrightAuth = updated.UsePlaywrightAuth,
            PlaywrightTimeoutSeconds = updated.PlaywrightTimeoutSeconds,
            MaxRetryAttempts = updated.MaxRetryAttempts,
            UpdatedAt = updated.UpdatedAt,
            UpdatedBy = updated.UpdatedBy
        });
    }

    /// <summary>
    /// POST /api/gis-integration/test-connection - Test GIS connection with current or provided credentials.
    /// </summary>
    [HttpPost("test-connection")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(GISConnectionTestResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> TestConnection([FromBody] TestGISConnectionRequest? request = null)
    {
        var result = await _authService.TestConnectionAsync(request?.Username, request?.Password);

        return Ok(new GISConnectionTestResultDto
        {
            IsSuccess = result.IsSuccess,
            Status = result.Status,
            Message = result.Message,
            ErrorMessage = result.ErrorMessage,
            ResponseTimeMs = result.ResponseTimeMs,
            SessionToken = result.SessionToken != null ? "****" + result.SessionToken[^8..] : null,
            TokenExpiresAt = result.TokenExpiresAt,
            TestedAt = result.TestedAt
        });
    }

    /// <summary>
    /// POST /api/gis-integration/authenticate - Authenticate to GIS and obtain session token.
    /// </summary>
    [HttpPost("authenticate")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(GISAuthenticationStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Authenticate()
    {
        var result = await _authService.AuthenticateAsync();

        if (!result.IsSuccess)
        {
            return BadRequest(new
            {
                isAuthenticated = false,
                status = "Failed",
                errorMessage = result.ErrorMessage
            });
        }

        return Ok(new GISAuthenticationStatusDto
        {
            IsAuthenticated = true,
            Status = "Authenticated",
            AuthenticatedAt = DateTime.UtcNow,
            TokenExpiresAt = result.TokenExpiresAt,
            TokenExpired = false,
            Username = _encryptionService.Mask(result.Username ?? ""),
            DepartmentName = result.DepartmentName
        });
    }

    /// <summary>
    /// GET /api/gis-integration/status - Get current GIS authentication status.
    /// </summary>
    [HttpGet("status")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(GISAuthenticationStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus()
    {
        var status = await _authService.GetSessionStatusAsync();

        return Ok(new GISAuthenticationStatusDto
        {
            IsAuthenticated = status.IsAuthenticated,
            Status = status.Status,
            AuthenticatedAt = status.AuthenticatedAt,
            TokenExpiresAt = status.TokenExpiresAt,
            TokenExpired = status.TokenExpiresAt.HasValue && status.TokenExpiresAt < DateTime.UtcNow,
            Username = status.Username != null ? _encryptionService.Mask(status.Username) : null,
            DepartmentName = status.DepartmentName
        });
    }

    /// <summary>
    /// DELETE /api/gis-integration/settings - Reset GIS integration settings.
    /// </summary>
    [HttpDelete("settings")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetSettings()
    {
        var currentUser = User.FindFirst("employeeId")?.Value ?? "unknown";

        await _auditService.LogActionAsync(action: "ResetGISIntegrationSettings",
            entityType: "GISIntegrationSettings",
            entityId: "all",
            changesJson: "GIS integration settings reset"
        );

        _logger.LogInformation("GIS integration settings reset by {User}", currentUser);

        return NoContent();
    }
}
