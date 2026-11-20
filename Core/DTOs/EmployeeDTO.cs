namespace WACAMS.Core.DTOs;

/// <summary>
/// Data transfer object for employee read operations.
/// Contains the full employee profile with department and manager names resolved.
/// </summary>
public record EmployeeDTO(
    Guid Id,
    string EmployeeId,
    string DisplayName,
    string? Email,
    string? Phone,
    string DepartmentName,
    string? ManagerName,
    Core.Enums.EmployeeStatus Status,
    DateTime HireDate,
    string? ShiftCode
);
