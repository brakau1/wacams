namespace WACAMS.Core.DTOs;

/// <summary>
/// Bulk upload request DTO for mass employee import.
/// Used when onboarding a batch of new employees or updating department assignments.
/// </summary>
public record BulkUploadDTO(
    List<EmployeeBulkItem> Employees
);

/// <summary>
/// Individual employee item within a bulk upload.
/// Each item represents one employee to be created or updated.
/// </summary>
public record EmployeeBulkItem(
    [Required]
    string EmployeeId,

    [Required]
    string DisplayName,

    string? Email,

    string? Phone,

    string DepartmentCode,

    string? ManagerId,

    string? ShiftCode
);
