namespace WACAMS.Api.Integration.GIS.DTOs;

/// <summary>
/// GIS Exception Detection Result DTO
/// </summary>
public class GISExceptionDetectionResultDTO
{
    public string EmployeeId { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty; // "LateArrival", "EarlyDeparture", "UnplannedAbsence", "MissingClockIn", "MissingClockOut"
    public DateTime ExceptionDate { get; set; }
    public string ShiftType { get; set; } = string.Empty;
    public int MinutesLost { get; set; }
    public string Severity { get; set; } = "Medium"; // "Low", "Medium", "High"
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
}
