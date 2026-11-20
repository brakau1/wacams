namespace WACAMS.Core.Enums;

/// <summary>
/// Represents the current stage of an escalation in the disciplinary chain.
/// </summary>
public enum EscalationStatus
{
    /// <summary>
    /// Escalation has been created and is awaiting acceptance by the next manager.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// The escalated manager has acknowledged receipt of the escalation.
    /// </summary>
    Accepted = 2,

    /// <summary>
    /// Required action has been completed by the escalated manager.
    /// </summary>
    ActionTaken = 3,

    /// <summary>
    /// Escalation could not be resolved at this level and is being pushed further up the hierarchy.
    /// </summary>
    Escalated = 4,

    /// <summary>
    /// Escalation is fully closed — action completed or case dismissed at final level.
    /// </summary>
    Closed = 5
}
