using System.ComponentModel.DataAnnotations;

namespace WACAMS.Core.Enums;

/// <summary>
/// Represents the approval status of an attendance exception.
/// </summary>
public enum ApprovalStatus
{
    /// <summary>
    /// Exception is pending review/approval.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Exception has been approved.
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Exception has been rejected.
    /// </summary>
    Rejected = 2
}
