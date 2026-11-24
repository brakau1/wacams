using System.ComponentModel.DataAnnotations;
using WACAMS.Core.Enums;
using WACAMS.Core.Exceptions.Models;

namespace WACAMS.Core.Entities;

/// <summary>
/// Represents an attendance exception detected for an employee on a specific date.
/// All disciplinary actions (warnings) MUST wait until investigation reaches "Investigated" or "Closed".
/// </summary>
public class AttendanceException
{
    /// <summary>
    /// Surrogate primary key — GUID internally.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Master key referencing the employee this exception belongs to.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EmployeeId { get; set; } = string.Empty;

    /// <summary>
    /// The date the exception occurred.
    /// </summary>
    [Required]
    public DateTime Date { get; set; }

    /// <summary>
    /// Alias for Date — used by some services/repositories as ExceptionDate.
    /// </summary>
    public DateTime ExceptionDate
    {
        get => Date;
        set => Date = value;
    }

    /// <summary>
    /// Current processing status of the exception (alias for InvestigationStatus).
    /// </summary>
    public InvestigationStatus Status
    {
        get => InvestigationStatus;
        set => InvestigationStatus = value;
    }

    /// <summary>
    /// The type of exception (LateArrival, EarlyDeparture, AWOL, etc.).
    /// </summary>
    [Required]
    public ExceptionType ExceptionType { get; set; }

    /// <summary>
    /// Free-text description of the exception details and context.
    /// </summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Current investigation status — controls whether disciplinary action can proceed.
    /// Must be "Investigated" or "Closed" before any WarningLetter can be generated.
    /// </summary>
    [Required]
    public InvestigationStatus InvestigationStatus { get; set; }

    /// <summary>
    /// Employee ID of the person who completed the investigation (Supervisor, HR, etc.).
    /// </summary>
    [MaxLength(50)]
    public string? InvestigatedBy { get; set; }

    /// <summary>
    /// Notes gathered during the investigation — evidence, employee statement, findings.
    /// </summary>
    [MaxLength(4000)]
    public string? InvestigationNotes { get; set; }

    /// <summary>
    /// Timestamp when the investigation was marked as complete.
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Expected clock-in time for this shift.
    /// </summary>
    public TimeSpan? ExpectedClockIn { get; set; }

    /// <summary>
    /// Actual clock-in time if recorded.
    /// </summary>
    public TimeSpan? ActualClockIn { get; set; }

    /// <summary>
    /// Expected clock-out time for this shift.
    /// </summary>
    public TimeSpan? ExpectedClockOut { get; set; }

    /// <summary>
    /// Actual clock-out time if recorded.
    /// </summary>
    public TimeSpan? ActualClockOut { get; set; }

    /// <summary>
    /// Minutes late (calculated from ExpectedClockIn vs ActualClockIn).
    /// </summary>
    public int MinutesLate { get; set; } = 0;

    /// <summary>
    /// Minutes early (calculated from ExpectedClockOut vs ActualClockOut).
    /// </summary>
    public int MinutesEarly { get; set; } = 0;

    /// <summary>
    /// Total lost production minutes due to this exception.
    /// </summary>
    public int LostProductionMinutes { get; set; } = 0;

    /// <summary>
    /// Timestamp when the investigation was initiated.
    /// </summary>
    public DateTime? InvestigatedAt { get; set; }

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Record last update timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property — the employee this exception belongs to.
    /// </summary>
    public Employee? Employee { get; set; }

    /// <summary>
    /// Navigation property — collection of warning letters that may result from this exception.
    /// </summary>
    public ICollection<WarningLetter> WarningLetters { get; set; } = new List<WarningLetter>();

    /// <summary>
    /// Approval status for the exception (Pending, Approved, Rejected).
    /// </summary>
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;

    /// <summary>
    /// Foreign key to the corrective action (if any) resulting from this exception.
    /// </summary>
    public Guid? CorrectiveActionId { get; set; }

    /// <summary>
    /// Exception category for approval workflow.
    /// </summary>
    public ExceptionCategory Category { get; set; }

    /// <summary>
    /// Employee ID who approved the exception.
    /// </summary>
    [MaxLength(50)]
    public string? ApprovedBy { get; set; }

    /// <summary>
    /// Date when the exception was approved/rejected.
    /// </summary>
    public DateTime? ApprovalDate { get; set; }

    /// <summary>
    /// Comments from the approver.
    /// </summary>
    [MaxLength(1000)]
    public string? ApprovalComments { get; set; }

    /// <summary>
    /// URL to supporting documentation.
    /// </summary>
    [MaxLength(500)]
    public string? SupportingDocumentUrl { get; set; }

    /// <summary>
    /// Employee ID who created the record.
    /// </summary>
    [MaxLength(50)]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Employee ID who last updated the record.
    /// </summary>
    [MaxLength(50)]
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Scheduled clock-in time for the shift.
    /// </summary>
    public TimeSpan? ScheduledClockIn { get; set; }

    /// <summary>
    /// Scheduled clock-out time for the shift.
    /// </summary>
    public TimeSpan? ScheduledClockOut { get; set; }

    /// <summary>
    /// Total lost minutes due to the exception.
    /// </summary>
    public int LostMinutes { get; set; }

    /// <summary>
    /// Total lost hours (computed from LostMinutes).
    /// </summary>
    public decimal LostHours => Math.Round(LostMinutes / 60.0m, 2);
}
