using System.ComponentModel.DataAnnotations;

namespace WACAMS.Api.DTOs;

/// <summary>
/// DTO for GIS integration settings (safe for API responses - excludes encrypted values).
/// </summary>
public class GISIntegrationSettingsDto
{
    public Guid Id { get; set; }
    public string PortalBaseUrl { get; set; } = string.Empty;
    public string IWorkApiBaseUrl { get; set; } = string.Empty;
    public string SsoEndpoint { get; set; } = string.Empty;
    public string? MaskedUsername { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public bool IsEnabled { get; set; }
    public int SyncIntervalMinutes { get; set; }
    public DateTime? LastConnectionTestAt { get; set; }
    public string? LastConnectionStatus { get; set; }
    public string? LastConnectionError { get; set; }
    public bool UsePlaywrightAuth { get; set; }
    public int PlaywrightTimeoutSeconds { get; set; }
    public int MaxRetryAttempts { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Request DTO for creating/updating GIS integration settings.
/// </summary>
public class UpdateGISIntegrationSettingsRequest
{
    [Required]
    [Url]
    public string PortalBaseUrl { get; set; } = string.Empty;

    [Required]
    [Url]
    public string IWorkApiBaseUrl { get; set; } = string.Empty;

    [Required]
    public string SsoEndpoint { get; set; } = "/public/sp/sso/yfjwt5?enterpriseId=yf";

    [Required]
    [MinLength(3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public bool IsEnabled { get; set; }
    public int SyncIntervalMinutes { get; set; } = 60;
    public bool UsePlaywrightAuth { get; set; } = true;
    public int PlaywrightTimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
}

/// <summary>
/// Response DTO for GIS connection test results.
/// </summary>
public class GISConnectionTestResultDto
{
    public bool IsSuccess { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime TestedAt { get; set; }
    public double ResponseTimeMs { get; set; }
    public string? SessionToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
}

/// <summary>
/// Response DTO for GIS authentication status.
/// </summary>
public class GISAuthenticationStatusDto
{
    public bool IsAuthenticated { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? AuthenticatedAt { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public bool TokenExpired { get; set; }
    public string? Username { get; set; }
    public string? DepartmentName { get; set; }
}

/// <summary>
/// Request DTO for testing GIS connection.
/// </summary>
public class TestGISConnectionRequest
{
    public string? Username { get; set; }
    public string? Password { get; set; }
}
