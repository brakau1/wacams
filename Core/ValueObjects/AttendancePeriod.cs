namespace WACAMS.Core.ValueObjects;

/// <summary>
/// Represents a defined attendance period with shift context.
/// Used to encapsulate expected clock-in/out times alongside the shift code for a specific date window.
/// Immutable value object — once created, its properties cannot be changed.
/// </summary>
public readonly record struct AttendancePeriod(
    /// <summary>
    /// Start date of the attendance period (inclusive).
    /// </summary>
    DateTime StartDate,

    /// <summary>
    /// End date of the attendance period (inclusive).
    /// </summary>
    DateTime EndDate,

    /// <summary>
    /// Shift code for this period (ES, MS, NS, LDS, DS, zdy).
    /// Maps to the employee's assigned shift or the specific ShiftSchedule entry.
    /// </summary>
    string ShiftCode,

    /// <summary>
    /// Expected clock-in time based on the shift pattern.
    /// ES/Morning: 06:00, MS/Afternoon: 14:00, NS/Night: 22:00.
    /// </summary>
    TimeSpan ExpectedClockIn,

    /// <summary>
    /// Expected clock-out time based on the shift pattern.
    /// ES/Morning: 14:00, MS/Afternoon: 22:00, NS/Night: 06:00 (next day).
    /// </summary>
    TimeSpan ExpectedClockOut
);
