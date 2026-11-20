using System.ComponentModel.DataAnnotations;

namespace WACAMS.Core.Escalations.Models;

/// <summary>
/// Escalation entity representing an escalation in the workflow
/// </summary>
public class Escalation
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // Reference to the exception being escalated
    [Required]
    public Guid ExceptionId { get; set; }

    // Employee information
    [Required]
    [MaxLength(50)]
    public string EmployeeId { get; set; } = string.Empty;

    // Current escalation level
    [Required]
    public EscalationLevel CurrentLevel { get; set; } = EscalationLevel.ManagerLevel1;

    // Current assignee
    [Required]
    [MaxLength(50)]
    public string AssignedTo { get; set; } = string.Empty;

    // Status
    [Required]
    public EscalationStatus Status { get; set; } = EscalationStatus.Pending;

    // Timing
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? ActionTakenAt { get; set; }
    public DateTime? EscalatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? AutoClosedAt { get; set; }

    // Next escalation time
    [Required]
    public DateTime NextEscalationTime { get; set; }

    // Document tracking
    public bool DocumentUploaded { get; set; } = false;

    [MaxLength(500)]
    public string? DocumentUrl { get; set; }

    public DateTime? DocumentUploadedAt { get; set; }

    // Comments and notes
    [MaxLength(2000)]
    public string? Notes { get; set; }

    [MaxLength(2000)]
    public string? ActionTakenNotes { get; set; }

    // Audit fields
    [Required]
    [MaxLength(50)]
    public string CreatedBy { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual Exceptions.Models.AttendanceException? Exception { get; set; }
    public virtual ICollection<EscalationHistory> History { get; set; } = new List<EscalationHistory>();
}
