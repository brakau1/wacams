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
    /// Foreign key referencing the AttendanceException being escalated.
    /// </summary>
    [Required]
    public Guid ExceptionId { get; set; }

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
    /// Employee ID of the person currently assigned to handle this escalation.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string AssignedTo { get; set; } = string.Empty;

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
    /// Timestamp when the escalation was acknowledged by the assignee.
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// Timestamp when action was taken on the escalation.
    /// </summary>
    public DateTime? ActionTakenAt { get; set; }

    /// <summary>
    /// Timestamp when the escalation was escalated to the next level.
    /// </summary>
    public DateTime? EscalatedAt { get; set; }

    /// <summary>
    /// Timestamp when the escalation was fully resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Timestamp when the escalation was automatically closed.
    /// </summary>
    public DateTime? AutoClosedAt { get; set; }

    /// <summary>
    /// Next scheduled escalation time.
    /// </summary>
    [Required]
    public DateTime NextEscalationTime { get; set; }

    /// <summary>
    /// Whether a document has been uploaded for this escalation.
    /// </summary>
    public bool DocumentUploaded { get; set; } = false;

    /// <summary>
    /// URL to the uploaded document.
    /// </summary>
    [MaxLength(500)]
    public string? DocumentUrl { get; set; }

    /// <summary>
    /// Timestamp when the document was uploaded.
    /// </summary>
    public DateTime? DocumentUploadedAt { get; set; }

    /// <summary>
    /// Notes on the action taken.
    /// </summary>
    [MaxLength(2000)]
    public string? ActionTakenNotes { get; set; }

    /// <summary>
    /// Employee ID of the person who created this escalation.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Employee ID of the person who last updated this escalation.
    /// </summary>
    [MaxLength(50)]
    public string? UpdatedBy { get; set; }

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

    /// <summary>
    /// Navigation property — the attendance exception being escalated.
    /// </summary>
    public AttendanceException? Exception { get; set; }

    /// <summary>
    /// Navigation property — history of escalation level changes.
    /// </summary>
    public virtual ICollection<EscalationHistory> History { get; set; } = new List<EscalationHistory>();
}
