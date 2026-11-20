using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WACAMS.Core.DTOs;
using WACAMS.Core.Enums;
using WACAMS.Core.Interfaces;

namespace WACAMS.Infrastructure.Services.NotificationChannels;

/// <summary>
/// Microsoft Teams notification channel implementation using Incoming Webhooks
/// </summary>
public class TeamsNotificationChannel : INotificationChannel
{
    private readonly ILogger<TeamsNotificationChannel> _logger;
    private readonly TeamsSettings _settings;
    private readonly HttpClient _httpClient;

    public NotificationChannel ChannelType => NotificationChannel.MicrosoftTeams;

    public TeamsNotificationChannel(
        ILogger<TeamsNotificationChannel> logger,
        IOptions<TeamsSettings> settings,
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
            // Build Teams Adaptive Card message
            var card = new
            {
                type = "message",
                attachments = new[]
                {
                    new
                    {
                        contentType = "application/vnd.microsoft.card.adaptive",
                        contentUrl = (string?)null,
                        content = new
                        {
                            type = "AdaptiveCard",
                            version = "1.4",
                            body = new object[]
                            {
                                new
                                {
                                    type = "TextBlock",
                                    text = request.Subject,
                                    weight = "Bolder",
                                    size = "Medium",
                                    wrap = true
                                },
                                new
                                {
                                    type = "TextBlock",
                                    text = request.Body,
                                    wrap = true,
                                    spacing = "Medium"
                                }
                            },
                            actions = BuildActions(request)
                        }
                    }
                }
            };

            // Determine webhook URL based on recipient (could be channel-specific)
            var webhookUrl = _settings.DefaultWebhookUrl;

            var response = await _httpClient.PostAsJsonAsync(webhookUrl, card, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Teams message sent successfully to {Recipient} for event {EventType}",
                    request.RecipientAddress, request.EventType);

                return NotificationSendResult.Successful(messageId: Guid.NewGuid().ToString());
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Teams webhook returned {StatusCode}: {Error}",
                    response.StatusCode, errorContent);

                return NotificationSendResult.Failed($"Teams delivery failed: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Teams message to {Recipient} for event {EventType}",
                request.RecipientAddress, request.EventType);

            return NotificationSendResult.Failed($"Teams delivery failed: {ex.Message}");
        }
    }

    public Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_settings.DefaultWebhookUrl))
            {
                _logger.LogWarning("Teams channel not configured: Default webhook URL is missing");
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Teams channel configuration validation failed");
            return Task.FromResult(false);
        }
    }

    private object[] BuildActions(NotificationSendRequest request)
    {
        var actions = new List<object>();

        // Add action button if there's a related entity
        if (!string.IsNullOrEmpty(request.RelatedEntityId))
        {
            var actionUrl = request.RelatedEntityType switch
            {
                "AttendanceException" => $"{_settings.AppBaseUrl}/exceptions/{request.RelatedEntityId}",
                "WarningLetter" => $"{_settings.AppBaseUrl}/warnings/{request.RelatedEntityId}",
                "Escalation" => $"{_settings.AppBaseUrl}/escalations/{request.RelatedEntityId}",
                _ => _settings.AppBaseUrl
            };

            actions.Add(new
            {
                type = "Action.OpenUrl",
                title = "View Details",
                url = actionUrl
            });
        }

        return actions.ToArray();
    }
}

/// <summary>
/// Microsoft Teams configuration settings
/// </summary>
public class TeamsSettings
{
    public string DefaultWebhookUrl { get; set; } = string.Empty;
    public string AppBaseUrl { get; set; } = string.Empty;
}
