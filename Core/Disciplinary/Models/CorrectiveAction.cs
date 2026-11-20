namespace WACAMS.Core.Disciplinary.Models;

/// <summary>
/// Corrective action entity for disciplinary management
/// </summary>
public class CorrectiveAction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // Employee and offence details
    public string EmployeeId { get; set; } = string.Empty;
    public Guid ExceptionId { get; set; }
    public OffenceType OffenceType { get; set; }
    public int OffenceNumber { get; set; } // 1st, 2nd, 3rd, etc.
    
    // Action details
    public DisciplinaryLevel Level { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    
    // Issuance
    public string IssuedBy { get; set; } = string.Empty; // Manager EmployeeId
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    
    // Acknowledgment
    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string? AcknowledgedBy { get; set; } // Employee ID who acknowledged
    
    // Status
    public CorrectiveActionStatus Status { get; set; } = CorrectiveActionStatus.Issued;
    public string? ResolutionNotes { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }
    
    // Validity and expiry
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime ValidUntil { get; set; }
    public bool IsExpired { get; set; }
    public DateTime? ExpiredAt { get; set; }
    
    // Dismissal hearing details (if applicable)
    public bool RequiresHearing { get; set; }
    public DateTime? HearingScheduledAt { get; set; }
    public string? HearingOutcome { get; set; }
    public DateTime? HearingConductedAt { get; set; }
    
    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Status of corrective action
/// </summary>
public enum CorrectiveActionStatus
{
    Issued,
    Acknowledged,
    Disputed,
    UnderHearing,
    Upheld,
    Overturned,
    Expired,
    Withdrawn
}
