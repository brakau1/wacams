namespace WACAMS.Core.Disciplinary.Models;

/// <summary>
/// Tracks offence count for an employee
/// </summary>
public class OffenceCounter
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EmployeeId { get; set; } = string.Empty;
    public OffenceType OffenceType { get; set; }
    public int TotalOffences { get; set; }
    public int ActiveOffences { get; set; } // Non-expired offences
    public DateTime LastOffenceDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
