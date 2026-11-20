namespace WACAMS.Core.RulesEngine.Models;

/// <summary>
/// Represents an attendance calculation result
/// </summary>
public class AttendanceCalculationResult
{
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime AttendanceDate { get; set; }
    public string ShiftType { get; set; } = string.Empty;
    
    // Time calculations
    public TimeSpan? ScheduledStart { get; set; }
    public TimeSpan? ActualStart { get; set; }
    public TimeSpan? ScheduledEnd { get; set; }
    public TimeSpan? ActualEnd { get; set; }
    
    // Calculated metrics
    public int LateMinutes { get; set; }
    public int EarlyMinutes { get; set; }
    public int LostMinutes { get; set; }
    public decimal LostHours => Math.Round(LostMinutes / 60.0m, 2);
    
    // Status
    public bool IsLate { get; set; }
    public bool IsEarlyDeparture { get; set; }
    public bool IsAbsent { get; set; }
    public bool IsPresent { get; set; }
    
    // Exception flags
    public bool HasException { get; set; }
    public string? ExceptionType { get; set; }
    public string? ExceptionDescription { get; set; }
}
