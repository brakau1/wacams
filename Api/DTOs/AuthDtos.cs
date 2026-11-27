using System.ComponentModel.DataAnnotations;

namespace WACAMS.Api.DTOs;

public class LoginRequest
{
    [Required]
    [StringLength(20)]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public EmployeeDto? Employee { get; set; }
}

public class RegisterRequest
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

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public Guid DepartmentId { get; set; }

    [StringLength(20)]
    public string? ManagerId { get; set; }

    [Required]
    public string Role { get; set; } = "Operator";
}

public class RefreshTokenRequest
{
    [Required]
    public string AccessToken { get; set; } = string.Empty;

    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}
