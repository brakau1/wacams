namespace WACAMS.Core.DTOs;

/// <summary>
/// Response DTO for successful authentication.
/// Contains JWT token, expiry time, and employee profile information.
/// </summary>
public record LoginResponseDTO(
    string Token,
    DateTime Expiry,
    string EmployeeId,
    string DisplayName,
    Core.Enums.Role Role
);
