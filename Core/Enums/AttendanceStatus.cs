namespace WACAMS.Core.Enums;

/// <summary>
/// Represents the attendance status of an employee for a specific work date.
/// </summary>
public enum AttendanceStatus
{
    /// <summary>
    /// Employee clocked in and out within expected parameters.
    /// </summary>
    Present = 1,

    /// <summary>
    /// Employee clocked in after the expected clock-in time (beyond grace period).
    /// </summary>
    Late = 2,

    /// <summary>
    /// Employee clocked out before the expected clock-out time.
    /// </summary>
    EarlyDeparture = 3,

    /// <summary>
    /// Employee did not clock in at all; no record found.
    /// </summary>
    Absent = 4,

    /// <summary>
    /// Employee is away without notice or authorization (Absent Without Leave).
    /// </summary>
    AWOL = 5,

    /// <summary>
    /// Employee is on approved sick leave.
    /// </summary>
    SickLeave = 6,

    /// <summary>
    /// Employee is on approved family responsibility leave.
    /// </summary>
    FamilyResponsibility = 7,

    /// <summary>
    /// Employee's scheduled shift was cancelled.
    /// </summary>
    ShiftCancellation = 8,

    /// <summary>
    /// Employee attended a workshop during scheduled hours.
    /// </summary>
    Workshop = 9,

    /// <summary>
    /// Employee attended training during scheduled hours.
    /// </summary>
    Training = 10,

    /// <summary>
    /// Clock-in record is missing from the system.
    /// </summary>
    MissingClockIn = 11,

    /// <summary>
    /// Clock-out record is missing from the system.
    /// </summary>
    MissingClockOut = 12,

    /// <summary>
    /// Attendance does not fit any defined category; requires manual review.
    /// </summary>
    Uncategorized = 13
}
