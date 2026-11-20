using System.ComponentModel.DataAnnotations;

namespace Wacam.Api.DTOs;

public class DepartmentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public string? ParentName { get; set; }
    public decimal TargetAttendancePercent { get; set; } = 95.0m;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateDepartmentRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public Guid? ParentId { get; set; }

    public decimal TargetAttendancePercent { get; set; } = 95.0m;
}

public class UpdateDepartmentRequest
{
    [StringLength(200)]
    public string? Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public Guid? ParentId { get; set; }

    public decimal? TargetAttendancePercent { get; set; }
}
