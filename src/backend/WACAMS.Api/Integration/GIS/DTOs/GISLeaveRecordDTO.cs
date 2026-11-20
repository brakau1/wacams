namespace WACAMS.Api.Integration.GIS.DTOs;

/// <summary>
/// GIS iWork Leave Record DTO
/// </summary>
public class GISLeaveRecordDTO
{
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime LeaveDate { get; set; }
    public string LeaveType { get; set; } = string.Empty; // "SickLeave", "AnnualLeave", "FamilyResponsibilityLeave"
    public string? LeaveReason { get; set; }
    public bool IsApproved { get; set; } = true;
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public string? SupportingDocument { get; set; }
}
