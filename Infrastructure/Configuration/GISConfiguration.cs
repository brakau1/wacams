namespace WACAMS.Infrastructure.Configuration;

/// <summary>
/// GIS iWork Integration Configuration
/// </summary>
public class GISConfiguration
{
    public const string SectionName = "GIS";

    /// <summary>
    /// GIS iWork API Base URL
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// API Key for authentication
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// API Secret for authentication
    /// </summary>
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>
    /// Company/Organization ID in GIS
    /// </summary>
    public string CompanyId { get; set; } = string.Empty;

    /// <summary>
    /// Sync interval in minutes (default: 60)
    /// </summary>
    public int SyncIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Maximum retry attempts for failed syncs (default: 3)
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts in seconds (default: 30)
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Request timeout in seconds (default: 30)
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Enable real-time webhook sync from GIS
    /// </summary>
    public bool EnableRealTimeSync { get; set; } = true;

    /// <summary>
    /// Enable scheduled background sync
    /// </summary>
    public bool EnableScheduledSync { get; set; } = true;

    /// <summary>
    /// Webhook secret for validating incoming webhooks
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// Late arrival grace period in minutes (default: 5)
    /// </summary>
    public int LateGraceMinutes { get; set; } = 5;

    /// <summary>
    /// Early departure grace period in minutes (default: 5)
    /// </summary>
    public int EarlyGraceMinutes { get; set; } = 5;

    /// <summary>
    /// Enable automatic exception detection
    /// </summary>
    public bool EnableExceptionDetection { get; set; } = true;
}
