using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WACAMS.Core.DTOs;
using WACAMS.Core.Enums;
using WACAMS.Core.Interfaces;

namespace WACAMS.Infrastructure.Services.NotificationChannels;

/// <summary>
/// WhatsApp notification channel implementation using Twilio WhatsApp API
/// </summary>
public class WhatsAppNotificationChannel : INotificationChannel
{
    private readonly ILogger<WhatsAppNotificationChannel> _logger;
    private readonly WhatsAppSettings _settings;
    private readonly HttpClient _httpClient;

    public NotificationChannel ChannelType => NotificationChannel.WhatsApp;

    public WhatsAppNotificationChannel(
        ILogger<WhatsAppNotificationChannel> logger,
        IOptions<WhatsAppSettings> settings,
        HttpClient httpClient)
    {
        _logger = logger;
        _settings = settings.Value;
        _httpClient = httpClient;
    }

    public async Task<NotificationSendResult> SendAsync(NotificationSendRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Format phone number for WhatsApp (add whatsapp: prefix)
            var toNumber = request.RecipientAddress.StartsWith("whatsapp:")
                ? request.RecipientAddress
                : $"whatsapp:{request.RecipientAddress}";

            var fromNumber = $"whatsapp:{_settings.FromNumber}";

            // Twilio WhatsApp API request
            var requestData = new Dictionary<string, string>
            {
                ["From"] = fromNumber,
                ["To"] = toNumber,
                ["Body"] = request.Body
            };

            // Set up Basic Authentication for Twilio
            var authToken = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{_settings.AccountSid}:{_settings.AuthToken}"));

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);

            var response = await _httpClient.PostAsJsonAsync(
                $"https://api.twilio.com/2010-04-01/Accounts/{_settings.AccountSid}/Messages.json",
                requestData,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TwilioMessageResponse>(cancellationToken);

                _logger.LogInformation("WhatsApp message sent successfully to {Recipient} for event {EventType}. SID: {Sid}",
                    request.RecipientAddress, request.EventType, result?.Sid);

                return NotificationSendResult.Successful(messageId: result?.Sid);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Twilio WhatsApp API returned {StatusCode}: {Error}",
                    response.StatusCode, errorContent);

                return NotificationSendResult.Failed($"WhatsApp delivery failed: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp message to {Recipient} for event {EventType}",
                request.RecipientAddress, request.EventType);

            return NotificationSendResult.Failed($"WhatsApp delivery failed: {ex.Message}");
        }
    }

    public Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_settings.AccountSid) ||
                string.IsNullOrWhiteSpace(_settings.AuthToken) ||
                string.IsNullOrWhiteSpace(_settings.FromNumber))
            {
                _logger.LogWarning("WhatsApp channel not configured: Twilio credentials or From number missing");
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp channel configuration validation failed");
            return Task.FromResult(false);
        }
    }

    private class TwilioMessageResponse
    {
        public string? Sid { get; set; }
        public string? Status { get; set; }
    }
}

/// <summary>
/// WhatsApp configuration settings (Twilio WhatsApp)
/// </summary>
public class WhatsAppSettings
{
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromNumber { get; set; } = string.Empty;
}
