using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WACAMS.Api.Integration.GIS.Models;

/// <summary>
/// GIS Shift Schedule Entity
/// </summary>
[Table("gis_shift_schedules")]
public class GISShiftSchedule
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(20)]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    public DateTime ScheduleDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string ShiftType { get; set; } = string.Empty;

    public TimeSpan ExpectedClockIn { get; set; }
    public TimeSpan ExpectedClockOut { get; set; }

    public bool IsWorkingDay { get; set; } = true;

    [MaxLength(50)]
    public string? ShiftCode { get; set; }

    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Index(nameof(EmployeeId), nameof(ScheduleDate), IsUnique = true)]
    public string UniqueKey => $"{EmployeeId}_{ScheduleDate:yyyy-MM-dd}";
}
