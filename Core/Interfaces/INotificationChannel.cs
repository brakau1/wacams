using WACAMS.Core.DTOs;
using WACAMS.Core.Enums;

namespace WACAMS.Core.Interfaces;

/// <summary>
/// Interface for notification channel implementations
/// </summary>
public interface INotificationChannel
{
    /// <summary>
    /// The channel type this implementation handles
    /// </summary>
    NotificationChannel ChannelType { get; }

    /// <summary>
    /// Send a notification through this channel
    /// </summary>
    Task<NotificationSendResult> SendAsync(NotificationSendRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate if the channel is properly configured
    /// </summary>
    Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default);
}
