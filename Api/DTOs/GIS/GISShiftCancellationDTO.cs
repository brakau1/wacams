namespace WACAMS.Api.Integration.GIS.DTOs;

/// <summary>
/// GIS iWork Shift Cancellation DTO
/// </summary>
public class GISShiftCancellationDTO
{
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime CancelledDate { get; set; }
    public string OriginalShiftType { get; set; } = string.Empty;
    public string CancellationReason { get; set; } = string.Empty;
    public DateTime CancelledAt { get; set; }
    public string CancelledBy { get; set; } = string.Empty;
    public bool IsApproved { get; set; } = true;
}
