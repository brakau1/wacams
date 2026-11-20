using System.ComponentModel.DataAnnotations;

namespace Wacam.Api.DTOs;

public class AddAdminRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string DisplayName { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;
}
