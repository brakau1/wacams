namespace WACAMS.Core.Enums;

/// <summary>
/// Represents the type of attendance exception recorded for an employee.
/// </summary>
public enum ExceptionType
{
    /// <summary>
    /// Employee arrived later than the expected clock-in time.
    /// </summary>
    LateArrival = 1,

    /// <summary>
    /// Employee departed earlier than the expected clock-out time.
    /// </summary>
    EarlyDeparture = 2,

    /// <summary>
    /// Employee was absent without a prior approved leave request.
    /// </summary>
    UnplannedAbsenteeism = 3,

    /// <summary>
    /// Employee is away without any notice or authorization.
    /// </summary>
    AWOL = 4,

    /// <summary>
    /// Employee is on sick leave (requires medical certificate).
    /// </summary>
    SickLeave = 5,

    /// <summary>
    /// Employee is on family responsibility leave (funeral, dependant matters).
    /// </summary>
    FamilyResponsibilityLeave = 6,

    /// <summary>
    /// The employee's scheduled shift was cancelled by management.
    /// </summary>
    ShiftCancellation = 7,

    /// <summary>
    /// Attendance at a workshop during scheduled working hours.
    /// </summary>
    WorkshopAttendance = 8,

    /// <summary>
    /// Attendance at training during scheduled working hours.
    /// </summary>
    Training = 9,

    /// <summary>
    /// Clock-in record was not captured by the attendance system.
    /// </summary>
    MissingClockIn = 10,

    /// <summary>
    /// Clock-out record was not captured by the attendance system.
    /// </summary>
    MissingClockOut = 11,

    /// <summary>
    /// Clock-in occurred outside of the scheduled shift window.
    /// </summary>
    UnscheduledClockIn = 12,

    /// <summary>
    /// Clock-out occurred outside of the scheduled shift window.
    /// </summary>
    UnscheduledClockOut = 13,

    /// <summary>
    /// Employee's actual attendance does not match the scheduled shift pattern.
    /// </summary>
    ScheduleMismatch = 14
}
