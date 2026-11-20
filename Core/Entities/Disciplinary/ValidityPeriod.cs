namespace WACAMS.Core.Disciplinary.Models;

/// <summary>
/// Configuration for validity periods of corrective actions
/// </summary>
public class ValidityPeriod
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DisciplinaryLevel Level { get; set; }
    public int ValidityDays { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
