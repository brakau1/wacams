namespace WACAMS.Core.DTOs;

/// <summary>
/// Data transfer object for attendance exception read operations.
/// Contains exception type, investigation status, and resolution details with employee name resolved.
/// </summary>
public record ExceptionDTO(
    Guid Id,
    string EmployeeId,
    string EmployeeName,
    DateTime Date,
    Core.Enums.ExceptionType ExceptionType,
    string? Description,
    Core.Enums.InvestigationStatus InvestigationStatus,
    string? InvestigatedBy,
    string? InvestigationNotes
);
