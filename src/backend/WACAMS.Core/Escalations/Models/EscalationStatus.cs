namespace WACAMS.Core.Escalations.Models;

/// <summary>
/// Status of an escalation
/// </summary>
public enum EscalationStatus
{
    Pending,
    Acknowledged,
    ActionTaken,
    Escalated,
    AutoClosed,
    Resolved
}
