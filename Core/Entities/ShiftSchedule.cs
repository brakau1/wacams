namespace WACAMS.Core.Entities;

/// <summary>
/// Represents a specific shift assignment for an employee on a given date.
/// Overrides the default ShiftCode in the Employee entity for that particular day.
/// </summary>
public class ShiftSchedule
{
    /// <summary>
    /// Surrogate primary key — GUID internally.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Master key referencing the employee this shift schedule belongs to.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EmployeeId { get; set; } = string.Empty;

    /// <summary>
    /// The date this shift schedule applies to (YYYY-MM-DD).
    /// </summary>
    [Required]
    public DateTime Date { get; set; }

    /// <summary>
    /// Shift code for this date (ES, MS, NS, LDS, DS, zdy).
    /// ES = Early/Morning 06:00-14:00
    /// MS = Middle/Afternoon 14:00-22:00
    /// NS = Night 22:00-06:00
    /// LDS = Long Day 06:00-18:00
    /// DS = Day 08:00-17:00
    /// zdy = Custom/Dynamic shift
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string ShiftCode { get; set; } = string.Empty;

    /// <summary>
    /// Actual start time of the shift for this date.
    /// Used to calculate expected clock-in and attendance windows.
    /// </summary>
    [Required]
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// Actual end time of the shift for this date.
    /// Used to calculate expected clock-out and early departure detection.
    /// </summary>
    [Required]
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// Whether this shift was cancelled by management.
    /// When true, the employee's attendance status should be marked as ShiftCancellation.
    /// </summary>
    public bool Cancelled { get; set; }

    /// <summary>
    /// Reason why the shift was cancelled (e.g., "Public holiday", "Equipment maintenance").
    /// Required when Cancelled is true.
    /// </summary>
    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    /// <summary>
    /// Navigation property — the employee this shift schedule belongs to.
    /// </summary>
    public Employee? Employee { get; set; }

    /// <summary>
    /// Navigation property — the attendance record associated with this shift schedule.
    /// One-to-one relationship: one shift schedule per attendance record per date.
    /// </summary>
    public AttendanceRecord? AttendanceRecord { get; set; }
}
