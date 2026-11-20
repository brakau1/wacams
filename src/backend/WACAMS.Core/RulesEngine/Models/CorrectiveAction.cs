namespace WACAMS.Core.RulesEngine.Models;

/// <summary>
/// Represents a corrective action for attendance violations
/// </summary>
public class CorrectiveAction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EmployeeId { get; set; } = string.Empty;
    public Guid ExceptionId { get; set; }
    
    // Action details
    public string ActionType { get; set; } = string.Empty; // Counselling, VerbalWarning, WrittenWarning, FinalWrittenWarning
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    
    // Issuance
    public string IssuedBy { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    
    // Acknowledgment
    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string? AcknowledgedBy { get; set; }
    
    // Status
    public CorrectiveActionStatus Status { get; set; } = CorrectiveActionStatus.Issued;
    public string? ResolutionNotes { get; set; }
    public DateTime? ResolvedAt { get; set; }
    
    // Expiry
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum CorrectiveActionStatus
{
    Issued,
    Acknowledged,
    Disputed,
    Resolved,
    Expired
}
