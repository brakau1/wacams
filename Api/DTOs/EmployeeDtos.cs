using System.ComponentModel.DataAnnotations;

namespace Wacam.Api.DTOs;

public class EmployeeDto
{
    public Guid Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateEmployeeRequest
{
    [Required]
    [StringLength(20)]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [StringLength(100)]
    public string? PhoneNumber { get; set; }

    [StringLength(20)]
    public string? ManagerId { get; set; }

    [Required]
    public Guid DepartmentId { get; set; }

    [Required]
    public string Role { get; set; } = "Operator";

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}

public class UpdateEmployeeRequest
{
    [StringLength(150)]
    public string? DisplayName { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(100)]
    public string? PhoneNumber { get; set; }

    [StringLength(20)]
    public string? ManagerId { get; set; }

    public Guid? DepartmentId { get; set; }

    [StringLength(50)]
    public string? Role { get; set; }

    public bool? IsActive { get; set; }
}

public class BulkUploadRequest
{
    public required List<BulkEmployeeItem> Employees { get; set; }
}

public class BulkEmployeeItem
{
    [Required]
    [StringLength(20)]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [StringLength(100)]
    public string? PhoneNumber { get; set; }

    [StringLength(20)]
    public string? ManagerId { get; set; }

    [Required]
    public Guid DepartmentId { get; set; }

    [Required]
    public string Role { get; set; } = "Operator";

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}

public class EmployeeHierarchyDto
{
    public EmployeeDto Employee { get; set; } = new();
    public EmployeeDto? Manager { get; set; }
    public List<EmployeeDto>? Subordinates { get; set; } = new();
    public List<string> HierarchyPath { get; set; } = new();
}
