using WACAMS.Core.RulesEngine.Models;

namespace WACAMS.Core.RulesEngine.Rules;

/// <summary>
/// Rule for determining exception severity
/// </summary>
public class ExceptionSeverityRule
{
    public string RuleName => "ExceptionSeverityDetermination";
    public string Description => "Determines the severity level of an attendance exception";

    public string DetermineSeverity(AttendanceCalculationResult calculation)
    {
        // High severity: Lost more than 2 hours or AWOL
        if (calculation.LostMinutes > 120 || calculation.IsAbsent)
        {
            return "High";
        }

        // Medium severity: Lost 30 minutes to 2 hours
        if (calculation.LostMinutes >= 30)
        {
            return "Medium";
        }

        // Low severity: Lost less than 30 minutes
        return "Low";
    }

    public bool RequiresImmediateAction(string severity)
    {
        return severity == "High";
    }
}
