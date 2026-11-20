using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WACAMS.Api.Integration.GIS.Models;

/// <summary>
/// GIS Sync Log Entity
/// </summary>
[Table("gis_sync_logs")]
public class GISSyncLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string SyncType { get; set; } = string.Empty; // "Employees", "Attendance", "Shifts", etc.

    public bool Success { get; set; }

    public int RecordsProcessed { get; set; }

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
}
