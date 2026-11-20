namespace WACAMS.Core.DTOs;

/// <summary>
/// Data transfer object for warning letter read operations.
/// Contains all disciplinary action details with employee name resolved.
/// </summary>
public record WarningLetterDTO(
    Guid Id,
    string EmployeeId,
    string EmployeeName,
    Core.Enums.WarningType Type,
    DateTime DateIssued,
    string Reason,
    string IssuedBy,
    bool Signed,
    string? SignedFileName
);
