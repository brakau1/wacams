namespace WACAMS.Core.Exceptions.Models;

/// <summary>
/// Categories of attendance exceptions
/// </summary>
public enum ExceptionCategory
{
    EarlyDeparture,
    UnplannedAbsenteeism,
    SickWhileAtWork,
    MedicalEmergency,
    FamilyResponsibility,
    Training,
    Workshop,
    ShiftCancellation,
    MissingClockIn,
    MissingClockOut,
    UnscheduledClockIn,
    UnscheduledClockOut
}
