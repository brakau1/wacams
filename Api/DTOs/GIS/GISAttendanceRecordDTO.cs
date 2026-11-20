namespace WACAMS.Api.Integration.GIS.DTOs;

/// <summary>
/// GIS iWork Attendance Record DTO
/// </summary>
public class GISAttendanceRecordDTO
{
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime AttendanceDate { get; set; }
    public string ShiftType { get; set; } = string.Empty;
    public TimeSpan? ExpectedClockIn { get; set; }
    public TimeSpan? ExpectedClockOut { get; set; }
    public TimeSpan? ActualClockIn { get; set; }
    public TimeSpan? ActualClockOut { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Source { get; set; } = "GIS_iWork";
    public Dictionary<string, object>? RawData { get; set; }
}
