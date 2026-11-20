using System.ComponentModel.DataAnnotations;

namespace WACAMS.Core.Exceptions.DTOs;

/// <summary>
/// Request to create a new attendance exception
/// </summary>
public class CreateExceptionRequest
{
    [Required]
    [MaxLength(50)]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    public DateTime ExceptionDate { get; set; }

    [Required]
    public ExceptionCategory Category { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public TimeSpan? ScheduledClockIn { get; set; }
    public TimeSpan? ActualClockIn { get; set; }
    public TimeSpan? ScheduledClockOut { get; set; }
    public TimeSpan? ActualClockOut { get; set; }

    public int LostMinutes { get; set; }

    [MaxLength(500)]
    public string? SupportingDocumentUrl { get; set; }

    [Required]
    [MaxLength(50)]
    public string CreatedBy { get; set; } = string.Empty;
}
