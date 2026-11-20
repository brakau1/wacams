namespace WACAMS.Core.Enums;

/// <summary>
/// Represents the result/status of a disciplinary corrective action.
/// </summary>
public enum DisciplinaryActionResult
{
    /// <summary>
    /// Action is pending review or implementation.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Action is currently active and in effect.
    /// </summary>
    Active = 2,

    /// <summary>
    /// Action has been completed successfully.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Action has been dismissed or overturned.
    /// </summary>
    Dismissed = 4,

    /// <summary>
    /// Action has expired due to time elapsed.
    /// </summary>
    Expired = 5,

    /// <summary>
    /// Action has been cancelled or withdrawn.
    /// </summary>
    Cancelled = 6
}
