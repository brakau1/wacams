using WACAMS.Core.Exceptions.Models;

namespace WACAMS.Core.Exceptions.DTOs;

/// <summary>
/// Response DTO for attendance exception
/// </summary>
public class ExceptionResponse
{
    public Guid Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime ExceptionDate { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // Time tracking
    public string? ScheduledClockIn { get; set; }
    public string? ActualClockIn { get; set; }
    public string? ScheduledClockOut { get; set; }
    public string? ActualClockOut { get; set; }
    
    // Lost time
    public int LostMinutes { get; set; }
    public decimal LostHours { get; set; }
    
    // Approval
    public string Status { get; set; } = string.Empty;
    public string? ApprovedBy { get; set; }
    public string? ApproverName { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public string? ApprovalComments { get; set; }
    
    // Documentation
    public string? SupportingDocumentUrl { get; set; }
    
    // Related action
    public Guid? CorrectiveActionId { get; set; }
    
    // Audit
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
