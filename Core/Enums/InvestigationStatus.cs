namespace WACAMS.Core.Enums;

/// <summary>
/// Represents the current stage of an attendance exception investigation.
/// Disciplinary action CANNOT be initiated until status reaches Investigated or Closed.
/// </summary>
public enum InvestigationStatus
{
    /// <summary>
    /// Exception has been detected but not yet assigned for review.
    /// </summary>
    Open = 1,

    /// <summary>
    /// Exception is currently under active review by a supervisor or HR representative.
    /// </summary>
    UnderReview = 2,

    /// <summary>
    /// Investigation is complete — evidence gathered, employee statement recorded.
    /// Disciplinary action may now be initiated if warranted.
    /// </summary>
    Investigated = 3,

    /// <summary>
    /// Case is fully resolved — investigation completed and corrective action taken or dismissed.
    /// </summary>
    Closed = 4
}
