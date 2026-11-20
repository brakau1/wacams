namespace WACAMS.Core.ValueObjects;

/// <summary>
/// Represents work duration metrics for a single attendance record or period.
/// Calculates actual hours worked, planned hours from the shift schedule, and overtime.
/// Immutable value object — once created, its properties cannot be changed.
/// </summary>
public readonly record struct WorkDuration(
    /// <summary>
    /// Actual hours worked between ClockInTime and ClockOutTime.
    /// Calculated as the difference in minutes divided by 60.0.
    /// Returns 0 if either clock time is null.
    /// </summary>
    decimal ActualHours,

    /// <summary>
    /// Planned hours based on the shift schedule (EndTime - StartTime).
    /// For night shifts crossing midnight, calculates correctly (e.g., 22:00→06:00 = 8 hours).
    /// </summary>
    decimal PlannedHours,

    /// <summary>
    /// Overtime hours — positive when ActualHours exceeds PlannedHours, zero otherwise.
    /// Formula: max(0, ActualHours - PlannedHours).
    /// Negative values are clamped to 0 (no "negative overtime").
    /// </summary>
    decimal OvertimeHours
);
