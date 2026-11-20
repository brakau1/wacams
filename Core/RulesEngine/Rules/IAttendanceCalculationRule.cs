namespace WACAMS.Core.RulesEngine.Rules;

/// <summary>
/// Interface for attendance calculation rules
/// </summary>
public interface IAttendanceCalculationRule
{
    string RuleName { get; }
    string Description { get; }
    bool CanCalculate(AttendanceCalculationContext context);
    AttendanceCalculationResult Calculate(AttendanceCalculationContext context);
}

/// <summary>
/// Context for attendance calculations
/// </summary>
public class AttendanceCalculationContext
{
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime AttendanceDate { get; set; }
    public string ShiftType { get; set; } = string.Empty;
    
    public TimeSpan? ScheduledStart { get; set; }
    public TimeSpan? ActualStart { get; set; }
    public TimeSpan? ScheduledEnd { get; set; }
    public TimeSpan? ActualEnd { get; set; }
    
    // Configuration
    public int LateGraceMinutes { get; set; } = 5;
    public int EarlyGraceMinutes { get; set; } = 5;
    public int LateThresholdMinutes { get; set; } = 30;
    public int HalfDayThresholdMinutes { get; set; } = 120;
    
    // Leave status
    public bool IsOnLeave { get; set; }
    public string? LeaveType { get; set; }
    
    // Shift cancellation
    public bool IsShiftCancelled { get; set; }
}
