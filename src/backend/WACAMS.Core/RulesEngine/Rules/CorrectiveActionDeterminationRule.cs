using WACAMS.Core.RulesEngine.Models;

namespace WACAMS.Core.RulesEngine.Rules;

/// <summary>
/// Rule for determining corrective action based on exception history
/// </summary>
public class CorrectiveActionDeterminationRule
{
    public string RuleName => "CorrectiveActionDetermination";
    public string Description => "Determines appropriate corrective action based on exception type and history";

    public CorrectiveAction DetermineAction(
        AttendanceException exception,
        int priorOccurrences)
    {
        var action = new CorrectiveAction
        {
            EmployeeId = exception.EmployeeId,
            ExceptionId = exception.Id,
            Severity = exception.Severity
        };

        // Determine action type based on exception type and occurrence count
        switch (exception.ExceptionType)
        {
            case "LateArrival":
            case "EarlyDeparture":
                action.ActionType = DetermineProgressiveAction(priorOccurrences);
                action.Description = GetProgressiveDescription(action.ActionType, priorOccurrences + 1);
                break;

            case "UnplannedAbsenteeism":
                action.ActionType = DetermineAbsenceAction(priorOccurrences);
                action.Description = GetAbsenceDescription(action.ActionType, priorOccurrences + 1);
                break;

            case "MissingClockIn":
            case "MissingClockOut":
                action.ActionType = DetermineMissingClockAction(priorOccurrences);
                action.Description = GetMissingClockDescription(action.ActionType, priorOccurrences + 1);
                break;

            default:
                action.ActionType = "Counselling";
                action.Description = "General attendance counselling";
                break;
        }

        // Set due date based on action type
        action.DueDate = GetDueDate(action.ActionType);

        // Set expiry date (typically 6 months for warnings)
        action.ExpiryDate = DateTime.UtcNow.AddMonths(6);

        return action;
    }

    private string DetermineProgressiveAction(int priorOccurrences)
    {
        return priorOccurrences switch
        {
            0 => "VerbalWarning",
            1 => "WrittenWarning",
            2 => "FinalWrittenWarning",
            _ => "Dismissal"
        };
    }

    private string DetermineAbsenceAction(int priorOccurrences)
    {
        return priorOccurrences switch
        {
            0 => "WrittenWarning",
            1 => "FinalWrittenWarning",
            _ => "Dismissal"
        };
    }

    private string DetermineMissingClockAction(int priorOccurrences)
    {
        return priorOccurrences switch
        {
            0 => "VerbalWarning",
            1 => "WrittenWarning",
            2 => "FinalWrittenWarning",
            _ => "Dismissal"
        };
    }

    private string GetProgressiveDescription(string actionType, int occurrence)
    {
        return $"{actionType} - Occurrence #{occurrence}";
    }

    private string GetAbsenceDescription(string actionType, int occurrence)
    {
        return $"{actionType} for Unplanned Absence - Occurrence #{occurrence}";
    }

    private string GetMissingClockDescription(string actionType, int occurrence)
    {
        return $"{actionType} for Missing Clock Transaction - Occurrence #{occurrence}";
    }

    private DateTime? GetDueDate(string actionType)
    {
        return actionType switch
        {
            "VerbalWarning" => DateTime.UtcNow.AddDays(7),
            "WrittenWarning" => DateTime.UtcNow.AddDays(14),
            "FinalWrittenWarning" => DateTime.UtcNow.AddDays(21),
            _ => null
        };
    }
}
