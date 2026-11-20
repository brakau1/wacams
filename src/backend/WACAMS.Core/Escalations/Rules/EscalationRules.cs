using WACAMS.Core.Escalations.Models;

namespace WACAMS.Core.Escalations.Rules;

/// <summary>
/// Configuration for escalation rules
/// </summary>
public class EscalationRules
{
    /// <summary>
    /// Hours before escalation to next level
    /// </summary>
    public Dictionary<EscalationLevel, int> EscalationHours { get; set; } = new()
    {
        { EscalationLevel.ManagerLevel1, 24 },
        { EscalationLevel.ManagerLevel2, 48 },
        { EscalationLevel.ManagerLevel3, 72 },
        { EscalationLevel.ManagerLevel4, 96 },
        { EscalationLevel.ManagerLevel5, 120 },
        { EscalationLevel.HR, 144 }
    };

    /// <summary>
    /// Gets the next escalation level
    /// </summary>
    public EscalationLevel? GetNextLevel(EscalationLevel currentLevel)
    {
        return currentLevel switch
        {
            EscalationLevel.ManagerLevel1 => EscalationLevel.ManagerLevel2,
            EscalationLevel.ManagerLevel2 => EscalationLevel.ManagerLevel3,
            EscalationLevel.ManagerLevel3 => EscalationLevel.ManagerLevel4,
            EscalationLevel.ManagerLevel4 => EscalationLevel.ManagerLevel5,
            EscalationLevel.ManagerLevel5 => EscalationLevel.HR,
            EscalationLevel.HR => EscalationLevel.Admin,
            EscalationLevel.Admin => null // Admin is final level
        };
    }

    /// <summary>
    /// Gets hours before escalation for a given level
    /// </summary>
    public int GetHoursBeforeEscalation(EscalationLevel level)
    {
        return EscalationHours.TryGetValue(level, out var hours) ? hours : 24;
    }

    /// <summary>
    /// Calculates next escalation time
    /// </summary>
    public DateTime CalculateNextEscalationTime(EscalationLevel currentLevel, DateTime fromTime)
    {
        var hours = GetHoursBeforeEscalation(currentLevel);
        return fromTime.AddHours(hours);
    }
}
