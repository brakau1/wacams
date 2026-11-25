using System.ComponentModel.DataAnnotations;

namespace WACAMS.Api.DTOs;

public class GenerateWarningRequest
{
    [Required]
    [StringLength(20)]
    public string EmployeeId { get; set; } = string.Empty;

    /// <summary>
    /// Exception ID that must be in "Investigated" or "Closed" status.
    /// </summary>
    [Required]
    public Guid ExceptionId { get; set; }

    [Required]
    [StringLength(50)]
    public string WarningType { get; set; } = string.Empty; // Counselling, VerbalWarning, WrittenWarning, FinalWrittenWarning

    [Required]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// EmployeeId of the issuing supervisor/team leader.
    /// </summary>
    [StringLength(20)]
    public string IssuedBy { get; set; } = string.Empty;
}

public class WarningLetterDto
{
    public Guid Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public Guid ExceptionId { get; set; }
    public string ExceptionType { get; set; } = string.Empty;
    public string WarningType { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string IssuedBy { get; set; } = string.Empty;
    public string? SignedFormPath { get; set; }
    public bool Acknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
