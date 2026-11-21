using System.ComponentModel.DataAnnotations;
using WACAMS.Core.Enums;

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
    /// Current approval status of the exception (alias for ApprovalStatus).
    /// </summary>
    public ApprovalStatus Status
    {
        get => ApprovalStatus;
        set => ApprovalStatus = value;
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
    public WACAMS.Core.Enums.ApprovalStatus ApprovalStatus { get; set; } = WACAMS.Core.Enums.ApprovalStatus.Pending;

    /// <summary>
    /// Foreign key to the corrective action (if any) resulting from this exception.
    /// </summary>
    public Guid? CorrectiveActionId { get; set; }

    /// <summary>
    /// Foreign key to the department where this exception occurred.
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Navigation property — the department where this exception occurred.
    /// </summary>
    public Department? Department { get; set; }

    /// <summary>
    /// Exception category (EarlyDeparture, UnplannedAbsenteeism, etc.).
    /// </summary>
    public Exceptions.Models.ExceptionCategory Category { get; set; }

    /// <summary>
    /// Alias for ExpectedClockIn.
    /// </summary>
    public TimeSpan? ScheduledClockIn
    {
        get => ExpectedClockIn;
        set => ExpectedClockIn = value;
    }

    /// <summary>
    /// Alias for ExpectedClockOut.
    /// </summary>
    public TimeSpan? ScheduledClockOut
    {
        get => ExpectedClockOut;
        set => ExpectedClockOut = value;
    }

    /// <summary>
    /// Alias for LostProductionMinutes.
    /// </summary>
    public int LostMinutes
    {
        get => LostProductionMinutes;
        set => LostProductionMinutes = value;
    }

    /// <summary>
    /// Lost hours (computed).
    /// </summary>
    public decimal LostHours => Math.Round(LostProductionMinutes / 60.0m, 2);

    /// <summary>
    /// Employee ID of the person who approved this exception.
    /// </summary>
    [MaxLength(50)]
    public string? ApprovedBy { get; set; }

    /// <summary>
    /// Date when the exception was approved/rejected.
    /// </summary>
    public DateTime? ApprovalDate { get; set; }

    /// <summary>
    /// Comments provided during approval/rejection.
    /// </summary>
    [MaxLength(1000)]
    public string? ApprovalComments { get; set; }

    /// <summary>
    /// URL to supporting documentation.
    /// </summary>
    [MaxLength(500)]
    public string? SupportingDocumentUrl { get; set; }

    /// <summary>
    /// Employee ID of the person who last updated this record.
    /// </summary>
    [MaxLength(50)]
    public string? UpdatedBy { get; set; }
}
