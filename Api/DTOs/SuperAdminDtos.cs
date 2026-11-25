using System.ComponentModel.DataAnnotations;
using WACAMS.Core.Entities;

namespace WACAMS.Api.DTOs;

#region User Management DTOs

public class UserManagementDto
{
    public Guid Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Department { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class RegisterUserRequest
{
    [Required]
    [MaxLength(50)]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Department { get; set; }

    [Required]
    public string Role { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    public bool MustChangePassword { get; set; } = true;
}

public class UpdateUserRequest
{
    [MaxLength(200)]
    public string? DisplayName { get; set; }

    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(100)]
    public string? Department { get; set; }

    public string? Role { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsLocked { get; set; }

    public bool? MustChangePassword { get; set; }
}

public class ResetPasswordRequest
{
    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;

    public bool ForceChangeOnNextLogin { get; set; } = true;
}

public class LockAccountRequest
{
    public string? Reason { get; set; }
}

public class UserLoginHistoryDto
{
    public DateTime LoginTime { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
}

#endregion

#region Role Management DTOs

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int UserCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class AssignRoleRequest
{
    [Required]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    public string? Reason { get; set; }
}

public class RemoveRoleRequest
{
    public string? Reason { get; set; }
}

#endregion

#region Access Control DTOs

public class AccessMatrixDto
{
    public string Role { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanApprove { get; set; }
    public bool CanExport { get; set; }
}

public class UpdateAccessMatrixRequest
{
    [Required]
    public string Role { get; set; } = string.Empty;

    [Required]
    public string Module { get; set; } = string.Empty;

    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanApprove { get; set; }
    public bool CanExport { get; set; }
}

#endregion

#region Employee Access Override DTOs

public class EmployeeOverrideDto
{
    public Guid Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string OverrideRole { get; set; } = string.Empty;
    public string? AttendanceRuleOverride { get; set; }
    public string? EscalationOverride { get; set; }
    public string? DisciplinaryOverride { get; set; }
    public string? DashboardOverride { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Notes { get; set; }
    public string CreatedByEmployeeId { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public bool IsActive { get; set; }
}

public class CreateEmployeeOverrideRequest
{
    [Required]
    [MaxLength(50)]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string OverrideRole { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? AttendanceRuleOverride { get; set; }

    [MaxLength(500)]
    public string? EscalationOverride { get; set; }

    [MaxLength(500)]
    public string? DisciplinaryOverride { get; set; }

    [MaxLength(500)]
    public string? DashboardOverride { get; set; }

    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;

    public DateTime? EffectiveTo { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class UpdateEmployeeOverrideRequest
{
    [MaxLength(50)]
    public string? OverrideRole { get; set; }

    [MaxLength(500)]
    public string? AttendanceRuleOverride { get; set; }

    [MaxLength(500)]
    public string? EscalationOverride { get; set; }

    [MaxLength(500)]
    public string? DisciplinaryOverride { get; set; }

    [MaxLength(500)]
    public string? DashboardOverride { get; set; }

    public DateTime? EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public bool? IsActive { get; set; }
}

#endregion

#region Audit Log DTOs

public class SecurityAuditLogDto
{
    public Guid Id { get; set; }
    public string ChangedEmployeeId { get; set; } = string.Empty;
    public string ChangedEmployeeName { get; set; } = string.Empty;
    public string? OldRole { get; set; }
    public string? NewRole { get; set; }
    public string ChangedByEmployeeId { get; set; } = string.Empty;
    public string ChangedByEmployeeName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime Timestamp { get; set; }
}

public class SecurityAuditQuery
{
    public string? EmployeeId { get; set; }
    public string? Action { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

#endregion

#region User Import DTOs

public class UserImportDto
{
    [Required]
    [MaxLength(50)]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Department { get; set; }

    [Required]
    public string Role { get; set; } = string.Empty;
}

public class UserImportResultDto
{
    public int TotalRows { get; set; }
    public int SuccessfulImports { get; set; }
    public int FailedImports { get; set; }
    public List<UserImportErrorDto> Errors { get; set; } = new();
}

public class UserImportErrorDto
{
    public int RowNumber { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

#endregion
