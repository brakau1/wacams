using System.ComponentModel.DataAnnotations;

namespace WACAMS.Core.Entities;

/// <summary>
/// GIS integration configuration settings.
/// Stores connection details, credentials (encrypted), and sync preferences.
/// </summary>
public class GISIntegrationSettings
{
    public Guid Id { get; set; }

    /// <summary>
    /// GIS Portal Base URL (e.g., https://gis.yfpo.com:9105)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string PortalBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// GIS iWork API Base URL (e.g., https://gis.yfpo.com:9120)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string IWorkApiBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// SSO Login Endpoint Path
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string SsoEndpoint { get; set; } = "/public/sp/sso/yfjwt5?enterpriseId=yf";

    /// <summary>
    /// GIS Username (encrypted)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string EncryptedUsername { get; set; } = string.Empty;

    /// <summary>
    /// GIS Password (encrypted)
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string EncryptedPassword { get; set; } = string.Empty;

    /// <summary>
    /// Department ID for data access scope
    /// </summary>
    public int? DepartmentId { get; set; }

    /// <summary>
    /// Department Name for display
    /// </summary>
    [MaxLength(200)]
    public string? DepartmentName { get; set; }

    /// <summary>
    /// Whether GIS integration is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Sync interval in minutes (0 = manual only)
    /// </summary>
    public int SyncIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Last successful connection test timestamp
    /// </summary>
    public DateTime? LastConnectionTestAt { get; set; }

    /// <summary>
    /// Last successful connection test result
    /// </summary>
    [MaxLength(50)]
    public string? LastConnectionStatus { get; set; }

    /// <summary>
    /// Last connection test error message
    /// </summary>
    [MaxLength(1000)]
    public string? LastConnectionError { get; set; }

    /// <summary>
    /// Current session token (JSESSIONID)
    /// </summary>
    [MaxLength(500)]
    public string? CurrentSessionToken { get; set; }

    /// <summary>
    /// Session token expiration timestamp
    /// </summary>
    public DateTime? SessionTokenExpiresAt { get; set; }

    /// <summary>
    /// Whether to use Playwright for authentication
    /// </summary>
    public bool UsePlaywrightAuth { get; set; } = true;

    /// <summary>
    /// Playwright browser timeout in seconds
    /// </summary>
    public int PlaywrightTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum retry attempts for authentication
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Timestamp when settings were last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who last updated settings
    /// </summary>
    [MaxLength(50)]
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Timestamp when settings were created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
