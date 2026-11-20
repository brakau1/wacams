namespace WACAMS.Core.Services;

/// <summary>
/// Domain service responsible for all attendance-related calculations.
/// Encapsulates business rules for late/early detection, lost time computation,
/// and attendance percentage formulas. All methods are pure functions — no side effects.
/// </summary>
public interface IAttendanceCalculationService
{
    /// <summary>
    /// Calculates the number of minutes an employee was late.
    /// Applies the grace period threshold — only minutes beyond the grace period count as late.
    /// If the employee clocked in before or at (expectedClockIn + gracePeriod), returns 0.
    /// Handles night shift crossing midnight correctly.
    /// </summary>
    /// <param name="expectedClockIn">The expected clock-in time based on the shift schedule.</param>
    /// <param name="actualClockIn">The actual clock-in time recorded by the system.</param>
    /// <returns>Number of late minutes (0 if within grace period or clocked in early).</returns>
    int CalculateLateMinutes(TimeSpan expectedClockIn, TimeSpan actualClockIn);

    /// <summary>
    /// Calculates the number of minutes an employee departed early.
    /// Only counts minutes before the expected clock-out time.
    /// If the employee stayed until or past expectedClockOut, returns 0.
    /// Handles night shift where end time may be earlier than start time (crosses midnight).
    /// </summary>
    /// <param name="expectedClockOut">The expected clock-out time based on the shift schedule.</param>
    /// <param name="actualClockOut">The actual clock-out time recorded by the system.</param>
    /// <returns>Number of early departure minutes (0 if they stayed until or past expected time).</returns>
    int CalculateEarlyMinutes(TimeSpan expectedClockOut, TimeSpan actualClockOut);

    /// <summary>
    /// Calculates total lost production hours from accumulated late and early minutes.
    /// Formula: (LateMinutes + EarlyMinutes) / 60.0
    /// Returns a decimal to preserve fractional hours (e.g., 45 minutes = 0.75 hours).
    /// </summary>
    /// <param name="lateMinutes">Total late minutes for the record or period.</param>
    /// <param name="earlyMinutes">Total early departure minutes for the record or period.</param>
    /// <returns>Total lost production hours as a decimal value.</returns>
    decimal CalculateLostHours(int lateMinutes, int earlyMinutes);

    /// <summary>
    /// Calculates the individual employee attendance percentage.
    /// Formula: (PresentDays / TotalWorkingDays) × 100
    /// Present days include Present status but exclude Late, Absent, AWOL, SickLeave, etc.
    /// Returns 0 if TotalWorkingDays is zero to avoid division by zero.
    /// </summary>
    /// <param name="presentDays">Number of days the employee had Present status.</param>
    /// <param name="totalDays">Total number of working days in the period.</param>
    /// <returns>Attendance percentage (0-100), rounded to 2 decimal places.</returns>
    decimal CalculateAttendancePercentage(int presentDays, int totalDays);

    /// <summary>
    /// Calculates the department-wide attendance percentage.
    /// Aggregates attendance data across all employees in the department.
    /// Formula: (Sum of all PresentDays across employees / Sum of all TotalWorkingDays across employees) × 100
    /// Provides a single metric for department-level performance dashboards.
    /// </summary>
    /// <param name="departmentRecords">Collection of per-employee attendance summaries for the department.</param>
    /// <param name="totalEmployees">Total number of active employees in the department.</param>
    /// <returns>Department attendance percentage (0-100), rounded to 2 decimal places.</returns>
    decimal CalculateDepartmentAttendancePercentage(
        IEnumerable<(int PresentDays, int TotalDays)> departmentRecords,
        int totalEmployees);
}
