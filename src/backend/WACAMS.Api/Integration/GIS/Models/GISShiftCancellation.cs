using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WACAMS.Api.Integration.GIS.Models;

/// <summary>
/// GIS Shift Cancellation Entity
/// </summary>
[Table("gis_shift_cancellations")]
public class GISShiftCancellation
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(20)]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    public DateTime CancelledDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string OriginalShiftType { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string CancellationReason { get; set; } = string.Empty;

    public DateTime CancelledAt { get; set; }

    [Required]
    [MaxLength(50)]
    public string CancelledBy { get; set; } = string.Empty;

    public bool IsApproved { get; set; } = true;

    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
