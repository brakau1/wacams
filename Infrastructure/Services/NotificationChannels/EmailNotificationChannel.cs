using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WACAMS.Core.DTOs;
using WACAMS.Core.Enums;
using WACAMS.Core.Interfaces;

namespace WACAMS.Infrastructure.Services.NotificationChannels;

/// <summary>
/// Email notification channel implementation
/// </summary>
public class EmailNotificationChannel : INotificationChannel
{
    private readonly ILogger<EmailNotificationChannel> _logger;
    private readonly EmailSettings _settings;
    private readonly SmtpClient _smtpClient;

    public NotificationChannel ChannelType => NotificationChannel.Email;

    public EmailNotificationChannel(
        ILogger<EmailNotificationChannel> logger,
        IOptions<EmailSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
        
        _smtpClient = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
        {
            Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword),
            EnableSsl = _settings.EnableSsl
        };
    }

    public async Task<NotificationSendResult> SendAsync(NotificationSendRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = request.Subject,
                Body = request.Body,
                IsBodyHtml = true
            };

            message.To.Add(request.RecipientAddress);

            // Set priority based on notification priority
            message.Priority = request.Priority switch
            {
                NotificationPriority.Urgent => MailPriority.High,
                NotificationPriority.High => MailPriority.High,
                NotificationPriority.Normal => MailPriority.Normal,
                NotificationPriority.Low => MailPriority.Low,
                _ => MailPriority.Normal
            };

            await _smtpClient.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("Email sent successfully to {Recipient} for event {EventType}",
                request.RecipientAddress, request.EventType);

            return NotificationSendResult.Successful(messageId: Guid.NewGuid().ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient} for event {EventType}",
                request.RecipientAddress, request.EventType);

            return NotificationSendResult.Failed($"Email delivery failed: {ex.Message}");
        }
    }

    public Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_settings.SmtpServer))
            {
                _logger.LogWarning("Email channel not configured: SMTP server is missing");
                return Task.FromResult(false);
            }

            if (string.IsNullOrWhiteSpace(_settings.FromEmail))
            {
                _logger.LogWarning("Email channel not configured: From email is missing");
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email channel configuration validation failed");
            return Task.FromResult(false);
        }
    }
}

/// <summary>
/// Email configuration settings
/// </summary>
public class EmailSettings
{
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "WACAMS System";
}
