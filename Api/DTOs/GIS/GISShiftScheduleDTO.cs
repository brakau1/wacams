namespace WACAMS.Api.Integration.GIS.DTOs;

/// <summary>
/// GIS iWork Shift Schedule DTO
/// </summary>
public class GISShiftScheduleDTO
{
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime ScheduleDate { get; set; }
    public string ShiftType { get; set; } = string.Empty;
    public TimeSpan ExpectedClockIn { get; set; }
    public TimeSpan ExpectedClockOut { get; set; }
    public bool IsWorkingDay { get; set; } = true;
    public string? ShiftCode { get; set; }
}
