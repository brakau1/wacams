using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WACAMS.Api.Integration.GIS.Models;

/// <summary>
/// GIS Employee Entity
/// </summary>
[Table("gis_employees")]
public class GISEmployee
{
    [Key]
    [MaxLength(20)]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(10)]
    public string? Gender { get; set; }

    [MaxLength(100)]
    public string? Nationality { get; set; }

    [MaxLength(20)]
    public string? DepartmentCode { get; set; }

    [MaxLength(200)]
    public string? DepartmentName { get; set; }

    [MaxLength(20)]
    public string? EmploymentStatus { get; set; }

    public DateTime? HireDate { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(20)]
    public string? ManagerId { get; set; }

    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
