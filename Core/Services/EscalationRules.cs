
namespace WACAMS.Core.Escalations.Rules;

/// <summary>
/// Configuration for escalation rules
/// </summary>
public class EscalationRules
{
    /// <summary>
    /// Hours before escalation to next level (by integer level)
    /// </summary>
    public Dictionary<int, int> EscalationHours { get; set; } = new()
    {
        { 1, 24 },
        { 2, 48 },
        { 3, 72 }
    };

    /// <summary>
    /// Gets the next escalation level
    /// </summary>
    public int? GetNextLevel(int currentLevel)
    {
        return currentLevel switch
        {
            1 => 2,
            2 => 3,
            3 => null // Max level reached
        };
    }

    /// <summary>
    /// Gets hours before escalation for a given level
    /// </summary>
    public int GetHoursBeforeEscalation(int level)
    {
        return EscalationHours.TryGetValue(level, out var hours) ? hours : 24;
    }

    /// <summary>
    /// Calculates next escalation time
    /// </summary>
    public DateTime CalculateNextEscalationTime(int currentLevel, DateTime fromTime)
    {
        var hours = GetHoursBeforeEscalation(currentLevel);
        return fromTime.AddHours(hours);
    }
}
