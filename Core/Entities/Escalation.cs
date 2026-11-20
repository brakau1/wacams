using WACAMS.Core.Enums;

namespace WACAMS.Core.Entities;

/// <summary>
/// Represents an escalation in the disciplinary chain when a manager fails to act within 24 hours.
/// Escalation follows the supervisor hierarchy — from the current manager up to their direct manager.
/// </summary>
public class Escalation
{
    /// <summary>
    /// Surrogate primary key — GUID internally.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key referencing the WarningLetter that triggered this escalation.
    /// </summary>
    [Required]
    public Guid WarningLetterId { get; set; }

    /// <summary>
    /// Employee ID of the manager who was supposed to act but did not (from manager level).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string FromManagerId { get; set; } = string.Empty;

    /// <summary>
    /// Employee ID of the next direct manager up the hierarchy who receives the escalation.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ToManagerId { get; set; } = string.Empty;

    /// <summary>
    /// Escalation level — 1 = first escalation (Team Leader → Supervisor), 2 = second (Supervisor → Manager), etc.
    /// Increments with each step up the hierarchy.
    /// </summary>
    [Required]
    public int Level { get; set; }

    /// <summary>
    /// Current status of the escalation — controls workflow progression.
    /// </summary>
    [Required]
    public EscalationStatus Status { get; set; }

    /// <summary>
    /// Description of the action required from the escalated manager.
    /// Carries forward the reason from the original warning letter.
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string ActionRequired { get; set; } = string.Empty;

    /// <summary>
    /// Master key referencing the employee this escalation concerns.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EmployeeId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the required action was completed by the escalated manager.
    /// Null while the escalation is still pending or in progress.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Notes on the escalation handling.
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Timestamp when the escalation was fully resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Record last update timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property — the employee this escalation concerns.
    /// </summary>
    public Employee? Employee { get; set; }

    /// <summary>
    /// Navigation property — the warning letter that triggered this escalation.
    /// </summary>
    public WarningLetter? WarningLetter { get; set; }
}
