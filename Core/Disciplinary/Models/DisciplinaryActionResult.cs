namespace WACAMS.Core.Disciplinary.Models;

/// <summary>
/// Result of disciplinary action determination
/// </summary>
public class DisciplinaryActionResult
{
    public bool Success { get; set; }
    public DisciplinaryLevel DeterminedLevel { get; set; }
    public int OffenceNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool RequiresManagerApproval { get; set; }
    public bool RequiresHearing { get; set; }
    public DateTime ValidUntil { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
