using System.ComponentModel.DataAnnotations;

namespace WACAMS.Core.Entities;

/// <summary>
/// History record for escalation level changes (EF Core entity).
/// </summary>
public class EscalationHistory
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid EscalationId { get; set; }

    [Required]
    public int FromLevel { get; set; }

    [Required]
    public int ToLevel { get; set; }

    [Required]
    [MaxLength(50)]
    public string FromAssignee { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ToAssignee { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Reason { get; set; }

    [Required]
    public DateTime EscalatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(50)]
    public string EscalatedBy { get; set; } = string.Empty;

    // Navigation property
    public virtual Escalation? Escalation { get; set; }
}
