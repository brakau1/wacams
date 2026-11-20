using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WACAMS.Core.Entities.GIS;

/// <summary>
/// GIS Leave Record Entity
/// </summary>
[Table("gis_leave_records")]
public class GISLeaveRecord
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(20)]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    public DateTime LeaveDate { get; set; }

    [Required]
    [MaxLength(50)]
    public string LeaveType { get; set; } = string.Empty; // "SickLeave", "AnnualLeave", "FamilyResponsibilityLeave"

    [MaxLength(1000)]
    public string? LeaveReason { get; set; }

    public bool IsApproved { get; set; } = true;

    public DateTime? ApprovedAt { get; set; }

    [MaxLength(50)]
    public string? ApprovedBy { get; set; }

    [MaxLength(500)]
    public string? SupportingDocument { get; set; }

    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string UniqueKey => $"{EmployeeId}_{LeaveDate:yyyy-MM-dd}";
}
