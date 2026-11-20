using WACAMS.Core.Enums;
using WACAMS.Core.Entities;

namespace WACAMS.Infrastructure.Services;

/// <summary>
/// Interface for sending escalation notifications
/// </summary>
public interface IEscalationNotificationService
{
    /// <summary>
    /// Send notification when a new escalation is created
    /// </summary>
    Task SendEscalationCreatedNotificationAsync(Escalation escalation);

    /// <summary>
    /// Send reminder notification when escalation is due (approaching deadline)
    /// </summary>
    Task SendEscalationDueNotificationAsync(Escalation escalation);

    /// <summary>
    /// Send notification when escalation moves to next level
    /// </summary>
    Task SendEscalationEscalatedNotificationAsync(
        Escalation escalation, 
        int fromLevel, 
        int toLevel);

    /// <summary>
    /// Send notification when escalation is closed (document uploaded or resolved)
    /// </summary>
    Task SendEscalationClosedNotificationAsync(Escalation escalation);

    /// <summary>
    /// Send notification when escalation is acknowledged
    /// </summary>
    Task SendEscalationAcknowledgedNotificationAsync(Escalation escalation);

    /// <summary>
    /// Send notification when action is taken on escalation
    /// </summary>
    Task SendActionTakenNotificationAsync(Escalation escalation);
}
