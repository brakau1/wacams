using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WACAMS.Core.DTOs;
using WACAMS.Core.Enums;
using WACAMS.Core.Interfaces;

namespace Infrastructure.Services.NotificationChannels;

/// <summary>
/// SMS notification channel implementation using Twilio
/// </summary>
public class SmsNotificationChannel : INotificationChannel
{
    private readonly ILogger<SmsNotificationChannel> _logger;
    private readonly SmsSettings _settings;
    private readonly HttpClient _httpClient;

    public NotificationChannel ChannelType => NotificationChannel.SMS;

    public SmsNotificationChannel(
        ILogger<SmsNotificationChannel> logger,
        IOptions<SmsSettings> settings,
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
            // Truncate message to SMS limit (160 chars for standard SMS)
            var messageBody = TruncateMessage(request.Body, 160);

            // Twilio API request
            var requestData = new Dictionary<string, string>
            {
                ["From"] = _settings.FromNumber,
                ["To"] = request.RecipientAddress,
                ["Body"] = messageBody
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

                _logger.LogInformation("SMS sent successfully to {Recipient} for event {EventType}. SID: {Sid}",
                    request.RecipientAddress, request.EventType, result?.Sid);

                return NotificationSendResult.Successful(messageId: result?.Sid);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Twilio API returned {StatusCode}: {Error}",
                    response.StatusCode, errorContent);

                return NotificationSendResult.Failed($"SMS delivery failed: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {Recipient} for event {EventType}",
                request.RecipientAddress, request.EventType);

            return NotificationSendResult.Failed($"SMS delivery failed: {ex.Message}");
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
                _logger.LogWarning("SMS channel not configured: Twilio credentials or From number missing");
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS channel configuration validation failed");
            return Task.FromResult(false);
        }
    }

    private string TruncateMessage(string message, int maxLength)
    {
        if (string.IsNullOrEmpty(message) || message.Length <= maxLength)
            return message;

        // Remove HTML tags for SMS
        var plainText = System.Text.RegularExpressions.Regex.Replace(message, "<.*?>", string.Empty);

        if (plainText.Length <= maxLength)
            return plainText;

        return plainText.Substring(0, maxLength - 3) + "...";
    }

    private class TwilioMessageResponse
    {
        public string? Sid { get; set; }
        public string? Status { get; set; }
    }
}

/// <summary>
/// SMS configuration settings (Twilio)
/// </summary>
public class SmsSettings
{
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromNumber { get; set; } = string.Empty;
}
