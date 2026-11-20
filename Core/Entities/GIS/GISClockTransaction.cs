using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WACAMS.Api.Integration.GIS.Models;

/// <summary>
/// GIS Clock Transaction Entity
/// </summary>
[Table("gis_clock_transactions")]
public class GISClockTransaction
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(20)]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    public DateTime TransactionDateTime { get; set; }

    [Required]
    [MaxLength(20)]
    public string TransactionType { get; set; } = string.Empty; // "ClockIn" or "ClockOut"

    [MaxLength(50)]
    public string? TerminalId { get; set; }

    [MaxLength(200)]
    public string? TerminalLocation { get; set; }

    [MaxLength(500)]
    public string? BiometricData { get; set; }

    public bool IsValid { get; set; } = true;

    [MaxLength(500)]
    public string? ValidationMessage { get; set; }

    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
