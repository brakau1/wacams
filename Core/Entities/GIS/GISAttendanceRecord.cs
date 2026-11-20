using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WACAMS.Core.Entities.GIS;

/// <summary>
/// GIS Attendance Record Entity
/// </summary>
[Table("gis_attendance_records")]
public class GISAttendanceRecord
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(20)]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    public DateTime AttendanceDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string ShiftType { get; set; } = string.Empty;

    public TimeSpan? ExpectedClockIn { get; set; }
    public TimeSpan? ExpectedClockOut { get; set; }
    public TimeSpan? ActualClockIn { get; set; }
    public TimeSpan? ActualClockOut { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Source { get; set; } = "GIS_iWork";

    public string? RawData { get; set; }

    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string UniqueKey => $"{EmployeeId}_{AttendanceDate:yyyy-MM-dd}_{ShiftType}";
}
