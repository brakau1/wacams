using System.ComponentModel.DataAnnotations;

namespace WACAMS.Core.Exceptions.Models;

/// <summary>
/// Attendance exception entity
/// </summary>
public class AttendanceException
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // Employee and date information
    [Required]
    [MaxLength(50)]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    public DateTime ExceptionDate { get; set; }

    // Exception details
    [Required]
    public ExceptionCategory Category { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    // Time tracking
    public TimeSpan? ScheduledClockIn { get; set; }
    public TimeSpan? ActualClockIn { get; set; }
    public TimeSpan? ScheduledClockOut { get; set; }
    public TimeSpan? ActualClockOut { get; set; }

    // Lost time calculation
    public int LostMinutes { get; set; }
    public decimal LostHours => Math.Round(LostMinutes / 60.0m, 2);

    // Approval workflow
    [Required]
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    [MaxLength(50)]
    public string? ApprovedBy { get; set; }

    public DateTime? ApprovalDate { get; set; }

    [MaxLength(1000)]
    public string? ApprovalComments { get; set; }

    // Supporting documentation
    [MaxLength(500)]
    public string? SupportingDocumentUrl { get; set; }

    // Related corrective action
    public Guid? CorrectiveActionId { get; set; }

    // Audit fields
    [Required]
    [MaxLength(50)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual Employee? Employee { get; set; }
    public virtual Disciplinary.Models.CorrectiveAction? CorrectiveAction { get; set; }
}
