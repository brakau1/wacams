using WACAMS.Core.Enums;

namespace WACAMS.Core.Entities;

/// <summary>
/// Represents a single day's attendance record for an employee.
/// Captures clock-in/out times, calculated lateness/early departure minutes, and overall status.
/// One record per employee per date.
/// </summary>
public class AttendanceRecord
{
    /// <summary>
    /// Surrogate primary key — GUID internally.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Master key referencing the employee this record belongs to.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EmployeeId { get; set; } = string.Empty;

    /// <summary>
    /// The work date this record pertains to (YYYY-MM-DD).
    /// </summary>
    [Required]
    public DateTime Date { get; set; }

    /// <summary>
    /// Actual clock-in time from the iWork/GIS attendance system or manual entry.
    /// Null if the employee did not clock in.
    /// </summary>
    public DateTime? ClockInTime { get; set; }

    /// <summary>
    /// Actual clock-out time from the iWork/GIS attendance system or manual entry.
    /// Null if the employee did not clock out.
    /// </summary>
    public DateTime? ClockOutTime { get; set; }

    /// <summary>
    /// Expected clock-in time based on the employee's shift schedule for this date.
    /// </summary>
    [Required]
    public TimeSpan ExpectedClockIn { get; set; }

    /// <summary>
    /// Expected clock-out time based on the employee's shift schedule for this date.
    /// </summary>
    [Required]
    public TimeSpan ExpectedClockOut { get; set; }

    /// <summary>
    /// Calculated late arrival minutes — how many minutes past ExpectedClockIn the employee arrived.
    /// Zero if the employee was on time or early. Not counted within grace period.
    /// </summary>
    public int LateMinutes { get; set; }

    /// <summary>
    /// Calculated early departure minutes — how many minutes before ExpectedClockOut the employee left.
    /// Zero if the employee stayed until or past the expected clock-out time.
    /// </summary>
    public int EarlyMinutes { get; set; }

    /// <summary>
    /// Overall attendance status for this day.
    /// </summary>
    [Required]
    public AttendanceStatus Status { get; set; }

    /// <summary>
    /// Source system where this record originated (e.g., "iWork", "Manual", "GIS").
    /// Useful for audit trail and data provenance.
    /// </summary>
    [MaxLength(50)]
    public string? SourceSystem { get; set; }

    /// <summary>
    /// Shift letter/code (A, B, C, ES, MS, NS, etc.)
    /// </summary>
    [MaxLength(10)]
    public string? ShiftCode { get; set; }

    /// <summary>
    /// Total hours worked calculated from clock-in and clock-out times.
    /// Handles overnight shifts automatically.
    /// </summary>
    public decimal TotalHoursWorked => CalculateTotalHours();

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Record last update timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property — the employee this record belongs to.
    /// </summary>
    public Employee? Employee { get; set; }

    /// <summary>
    /// Navigation property — the shift schedule associated with this attendance record.
    /// </summary>
    public ShiftSchedule? ShiftSchedule { get; set; }

    /// <summary>
    /// Calculates total hours worked from clock-in and clock-out times.
    /// Handles overnight shifts by adding 24 hours if end time is before start time.
    /// </summary>
    private decimal CalculateTotalHours()
    {
        if (ClockInTime.HasValue && ClockOutTime.HasValue)
        {
            var diff = ClockOutTime.Value - ClockInTime.Value;
            if (diff.TotalHours < 0) diff += TimeSpan.FromDays(1);
            return (decimal)Math.Round(diff.TotalHours, 2);
        }
        return 0;
    }
}
