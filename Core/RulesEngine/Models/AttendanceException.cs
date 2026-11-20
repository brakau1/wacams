namespace WACAMS.Core.RulesEngine.Models;

/// <summary>
/// Represents an attendance exception
/// </summary>
public class AttendanceException
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime ExceptionDate { get; set; }
    public string ExceptionType { get; set; } = string.Empty;
    public string ShiftType { get; set; } = string.Empty;
    
    // Metrics
    public int MinutesLost { get; set; }
    public string Severity { get; set; } = "Medium"; // Low, Medium, High
    
    // Investigation workflow
    public ExceptionStatus Status { get; set; } = ExceptionStatus.Pending;
    public string? InvestigatedBy { get; set; }
    public DateTime? InvestigatedAt { get; set; }
    public string? InvestigationNotes { get; set; }
    public string? InvestigationOutcome { get; set; }
    
    // Decision
    public bool IsApproved { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    
    // Corrective action
    public bool RequiresCorrectiveAction { get; set; }
    public Guid? CorrectiveActionId { get; set; }
    
    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}

public enum ExceptionStatus
{
    Pending,
    UnderInvestigation,
    Approved,
    Rejected,
    Escalated
}
