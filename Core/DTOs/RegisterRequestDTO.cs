namespace WACAMS.Core.DTOs;

/// <summary>
/// Request DTO for creating a new user account.
/// Used by Admin or HR to register an employee with system access.
/// </summary>
public record RegisterRequestDTO(
    [Required]
    string Username,

    [Required]
    string Password,

    [Required]
    string EmployeeId,

    [Required]
    Core.Enums.Role Role
);
