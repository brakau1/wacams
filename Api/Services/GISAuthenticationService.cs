using Microsoft.EntityFrameworkCore;
using WACAMS.Api.Data;
using WACAMS.Core.Entities;

namespace WACAMS.Api.Services;

/// <summary>
/// Service for authenticating to GIS portal and managing session tokens.
/// Phase 4A: Authentication infrastructure only (no data sync yet).
/// </summary>
public class GISAuthenticationService
{
    private readonly WacamDbContext _context;
    private readonly EncryptionService _encryptionService;
    private readonly AuditService _auditService;
    private readonly ILogger<GISAuthenticationService> _logger;
    private readonly HttpClient _httpClient;

    public GISAuthenticationService(
        WacamDbContext context,
        EncryptionService encryptionService,
        AuditService auditService,
        ILogger<GISAuthenticationService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _encryptionService = encryptionService;
        _auditService = auditService;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("GISClient");
    }

    /// <summary>
    /// Get current GIS integration settings.
    /// </summary>
    public async Task<GISIntegrationSettings?> GetSettingsAsync()
    {
        return await _context.GISIntegrationSettings.FirstOrDefaultAsync();
    }

    /// <summary>
    /// Update GIS integration settings.
    /// </summary>
    public async Task<GISIntegrationSettings> UpdateSettingsAsync(
        GISIntegrationSettings settings,
        string? username,
        string? password,
        string updatedBy)
    {
        var existing = await _context.GISIntegrationSettings.FirstOrDefaultAsync();

        if (existing == null)
        {
            // Create new settings
            settings.EncryptedUsername = _encryptionService.Encrypt(username ?? "");
            settings.EncryptedPassword = _encryptionService.Encrypt(password ?? "");
            settings.UpdatedBy = updatedBy;
            settings.CreatedAt = DateTime.UtcNow;
            settings.UpdatedAt = DateTime.UtcNow;

            _context.GISIntegrationSettings.Add(settings);
        }
        else
        {
            // Update existing settings
            existing.PortalBaseUrl = settings.PortalBaseUrl;
            existing.IWorkApiBaseUrl = settings.IWorkApiBaseUrl;
            existing.SsoEndpoint = settings.SsoEndpoint;
            existing.DepartmentId = settings.DepartmentId;
            existing.DepartmentName = settings.DepartmentName;
            existing.IsEnabled = settings.IsEnabled;
            existing.SyncIntervalMinutes = settings.SyncIntervalMinutes;
            existing.UsePlaywrightAuth = settings.UsePlaywrightAuth;
            existing.PlaywrightTimeoutSeconds = settings.PlaywrightTimeoutSeconds;
            existing.MaxRetryAttempts = settings.MaxRetryAttempts;
            existing.UpdatedBy = updatedBy;
            existing.UpdatedAt = DateTime.UtcNow;

            // Update credentials if provided
            if (!string.IsNullOrEmpty(username))
                existing.EncryptedUsername = _encryptionService.Encrypt(username);

            if (!string.IsNullOrEmpty(password))
                existing.EncryptedPassword = _encryptionService.Encrypt(password);
        }

        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(
            actorId: updatedBy,
            action: "UpdateGISIntegrationSettings",
            entityType: "GISIntegrationSettings",
            entityId: settings.Id.ToString(),
            changesJson: $"Updated GIS integration settings"
        );

        return settings;
    }

    /// <summary>
    /// Authenticate to GIS portal and obtain session token.
    /// Phase 4A: Returns mock authentication result (Playwright implementation in Phase 4B).
    /// </summary>
    public async Task<GISAuthenticationResult> AuthenticateAsync()
    {
        var settings = await GetSettingsAsync();

        if (settings == null)
        {
            _logger.LogWarning("GIS integration settings not configured");
            return new GISAuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = "GIS integration not configured"
            };
        }

        if (!settings.IsEnabled)
        {
            _logger.LogWarning("GIS integration is disabled");
            return new GISAuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = "GIS integration is disabled"
            };
        }

        try
        {
            // Decrypt credentials
            var username = _encryptionService.Decrypt(settings.EncryptedUsername);
            var password = _encryptionService.Decrypt(settings.EncryptedPassword);

            _logger.LogInformation("Attempting GIS authentication for user: {Username}", 
                _encryptionService.Mask(username));

            // Phase 4A: Mock authentication (Playwright implementation in Phase 4B)
            // In Phase 4B, this will use Playwright to:
            // 1. Navigate to GIS SSO portal
            // 2. Fill login form
            // 3. Extract session cookies
            // 4. Return session token

            var mockSessionToken = $"JSESSIONID={Guid.NewGuid()}";
            var tokenExpiresAt = DateTime.UtcNow.AddHours(2); // Typical GIS session timeout

            // Update settings with session token
            settings.CurrentSessionToken = mockSessionToken;
            settings.SessionTokenExpiresAt = tokenExpiresAt;
            settings.LastConnectionTestAt = DateTime.UtcNow;
            settings.LastConnectionStatus = "Connected";
            settings.LastConnectionError = null;
            settings.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(
                actorId: "system",
                action: "GISAuthentication",
                entityType: "GISIntegrationSettings",
                entityId: settings.Id.ToString(),
                changesJson: "GIS authentication successful"
            );

            _logger.LogInformation("GIS authentication successful, session expires at: {ExpiresAt}", 
                tokenExpiresAt);

            return new GISAuthenticationResult
            {
                IsSuccess = true,
                SessionToken = mockSessionToken,
                TokenExpiresAt = tokenExpiresAt,
                Username = username,
                DepartmentName = settings.DepartmentName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GIS authentication failed");

            settings.LastConnectionTestAt = DateTime.UtcNow;
            settings.LastConnectionStatus = "Failed";
            settings.LastConnectionError = ex.Message;
            settings.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new GISAuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Check if current GIS session is valid.
    /// </summary>
    public async Task<GISSessionStatus> GetSessionStatusAsync()
    {
        var settings = await GetSettingsAsync();

        if (settings == null)
        {
            return new GISSessionStatus
            {
                IsAuthenticated = false,
                Status = "Not Configured"
            };
        }

        if (string.IsNullOrEmpty(settings.CurrentSessionToken))
        {
            return new GISSessionStatus
            {
                IsAuthenticated = false,
                Status = "Not Authenticated"
            };
        }

        if (settings.SessionTokenExpiresAt.HasValue && settings.SessionTokenExpiresAt < DateTime.UtcNow)
        {
            return new GISSessionStatus
            {
                IsAuthenticated = false,
                Status = "Token Expired",
                TokenExpiresAt = settings.SessionTokenExpiresAt
            };
        }

        return new GISSessionStatus
        {
            IsAuthenticated = true,
            Status = "Active",
            AuthenticatedAt = settings.LastConnectionTestAt,
            TokenExpiresAt = settings.SessionTokenExpiresAt,
            Username = _encryptionService.Decrypt(settings.EncryptedUsername),
            DepartmentName = settings.DepartmentName
        };
    }

    /// <summary>
    /// Test GIS connection with provided or stored credentials.
    /// </summary>
    public async Task<GISConnectionTestResult> TestConnectionAsync(string? username = null, string? password = null)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var settings = await GetSettingsAsync();

            // Use provided credentials or decrypt stored ones
            string actualUsername;
            string actualPassword;

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                actualUsername = username;
                actualPassword = password;
            }
            else if (settings != null)
            {
                actualUsername = _encryptionService.Decrypt(settings.EncryptedUsername);
                actualPassword = _encryptionService.Decrypt(settings.EncryptedPassword);
            }
            else
            {
                return new GISConnectionTestResult
                {
                    IsSuccess = false,
                    ErrorMessage = "No credentials provided and no settings configured",
                    TestedAt = DateTime.UtcNow
                };
            }

            _logger.LogInformation("Testing GIS connection for user: {Username}", 
                _encryptionService.Mask(actualUsername));

            // Phase 4A: Mock connection test
            // In Phase 4B, this will use Playwright to actually test the connection

            await Task.Delay(500); // Simulate network delay

            var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            var mockSessionToken = $"JSESSIONID={Guid.NewGuid()}";
            var tokenExpiresAt = DateTime.UtcNow.AddHours(2);

            // Update settings if using stored credentials
            if (settings != null && string.IsNullOrEmpty(username))
            {
                settings.CurrentSessionToken = mockSessionToken;
                settings.SessionTokenExpiresAt = tokenExpiresAt;
                settings.LastConnectionTestAt = DateTime.UtcNow;
                settings.LastConnectionStatus = "Connected";
                settings.LastConnectionError = null;
                settings.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }

            await _auditService.LogActionAsync(
                actorId: "system",
                action: "GISConnectionTest",
                entityType: "GISIntegrationSettings",
                entityId: settings?.Id.ToString() ?? "test",
                changesJson: "GIS connection test successful"
            );

            return new GISConnectionTestResult
            {
                IsSuccess = true,
                Status = "Connected",
                Message = "Successfully connected to GIS portal",
                ResponseTimeMs = responseTime,
                SessionToken = mockSessionToken,
                TokenExpiresAt = tokenExpiresAt,
                TestedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GIS connection test failed");

            return new GISConnectionTestResult
            {
                IsSuccess = false,
                Status = "Failed",
                ErrorMessage = ex.Message,
                ResponseTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds,
                TestedAt = DateTime.UtcNow
            };
        }
    }
}

/// <summary>
/// Result of GIS authentication attempt.
/// </summary>
public class GISAuthenticationResult
{
    public bool IsSuccess { get; set; }
    public string? SessionToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public string? Username { get; set; }
    public string? DepartmentName { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Current GIS session status.
/// </summary>
public class GISSessionStatus
{
    public bool IsAuthenticated { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? AuthenticatedAt { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public string? Username { get; set; }
    public string? DepartmentName { get; set; }
}

/// <summary>
/// Result of GIS connection test.
/// </summary>
public class GISConnectionTestResult
{
    public bool IsSuccess { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
    public double ResponseTimeMs { get; set; }
    public string? SessionToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public DateTime TestedAt { get; set; }
}
