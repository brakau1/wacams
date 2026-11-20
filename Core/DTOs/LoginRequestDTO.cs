namespace WACAMS.Core.DTOs;

/// <summary>
/// Request DTO for user authentication.
/// Contains login credentials submitted via the API endpoint.
/// </summary>
public record LoginRequestDTO(
    [Required]
    string Username,

    [Required]
    string Password
);
