namespace WACAMS.Core.DTOs;

/// <summary>
/// Request DTO for filtering attendance and exception queries.
/// Supports multi-criteria filtering on date ranges, departments, employees, statuses, and thresholds.
/// </summary>
public record FilterOptionsDTO(
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    Guid[]? DepartmentIds = null,
    string[]? EmployeeIds = null,
    Core.Enums.AttendanceStatus[]? Statuses = null,
    int? MinLateMinutes = null,
    int? MinEarlyMinutes = null
);
