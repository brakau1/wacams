using System.ComponentModel.DataAnnotations;

namespace WACAMS.Core.Exceptions.DTOs;

/// <summary>
/// Request to approve or reject an attendance exception
/// </summary>
public class ApprovalRequest
{
    [Required]
    public Guid ExceptionId { get; set; }

    [Required]
    public bool IsApproved { get; set; }

    [MaxLength(50)]
    public string ApprovedBy { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Comments { get; set; }
}
