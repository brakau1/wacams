using WACAMS.Core.Enums;

namespace WACAMS.Core.Entities;

/// <summary>
/// Represents a disciplinary warning letter issued to an employee.
/// Can only be created after an AttendanceException has been investigated (status = Investigated or Closed).
/// Supports progressive discipline: Counselling → Verbal → Written → Final Written.
/// </summary>
public class WarningLetter
{
    /// <summary>
    /// Surrogate primary key — GUID internally.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Master key referencing the employee receiving the warning.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EmployeeId { get; set; } = string.Empty;

    /// <summary>
    /// The type of warning being issued (progressive discipline level).
    /// </summary>
    [Required]
    public WarningType Type { get; set; }

    /// <summary>
    /// Date the warning letter was formally issued to the employee.
    /// </summary>
    [Required]
    public DateTime DateIssued { get; set; }

    /// <summary>
    /// Detailed reason for the warning, referencing specific attendance exceptions and investigation findings.
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Employee ID of the manager/supervisor who issued this warning.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string IssuedBy { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the employee has signed and acknowledged the warning letter.
    /// </summary>
    public bool Signed { get; set; }

    /// <summary>
    /// Filename of the signed warning document stored in the file system or cloud storage.
    /// Example: "warning_708513_2026-11-12_verbal.pdf"
    /// </summary>
    [MaxLength(255)]
    public string? SignedFileName { get; set; }

    /// <summary>
    /// Full file path or URI where the signed warning document is stored.
    /// </summary>
    [MaxLength(1000)]
    public string? SignedFilePath { get; set; }

    /// <summary>
    /// Free-text field for any additional acknowledgements or conditions noted by the employee during signing.
    /// </summary>
    [MaxLength(1000)]
    public string? Acknowledgements { get; set; }

    /// <summary>
    /// Next review date — when management should reassess the employee's attendance performance.
    /// </summary>
    public DateTime? NextReviewDate { get; set; }

    /// <summary>
    /// Foreign key to the attendance exception that triggered this warning.
    /// </summary>
    public Guid ExceptionId { get; set; }

    /// <summary>
    /// Timestamp when the employee acknowledged the warning.
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Record last update timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property — the attendance exception that triggered this warning.
    /// </summary>
    public AttendanceException? Exception { get; set; }

    /// <summary>
    /// Navigation property — the employee this warning is issued to.
    /// </summary>
    public Employee? Employee { get; set; }

    /// <summary>
    /// Navigation property — collection of escalations triggered by this warning.
    /// </summary>
    public ICollection<Escalation> Escalations { get; set; } = new List<Escalation>();
}
