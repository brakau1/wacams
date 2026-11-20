using WACAMS.Core.Disciplinary.Models;

namespace WACAMS.Core.Disciplinary.Rules;

/// <summary>
/// Progressive discipline rules engine
/// </summary>
public class ProgressiveDisciplineRule
{
    /// <summary>
    /// Determines the disciplinary level based on offence count
    /// </summary>
    public DisciplinaryLevel DetermineLevel(OffenceType offenceType, int offenceCount)
    {
        // Late Arrival and Early Departure follow the same progression
        if (offenceType == OffenceType.LateArrival || offenceType == OffenceType.EarlyDeparture)
        {
            return offenceCount switch
            {
                1 => DisciplinaryLevel.Counselling,
                2 => DisciplinaryLevel.VerbalWarning,
                3 => DisciplinaryLevel.WrittenWarning,
                4 => DisciplinaryLevel.FinalWrittenWarning,
                >= 5 => DisciplinaryLevel.DismissalHearing,
                _ => DisciplinaryLevel.None
            };
        }

        // Other offence types can have different progressions
        return offenceCount switch
        {
            1 => DisciplinaryLevel.VerbalWarning,
            2 => DisciplinaryLevel.WrittenWarning,
            3 => DisciplinaryLevel.FinalWrittenWarning,
            >= 4 => DisciplinaryLevel.DismissalHearing,
            _ => DisciplinaryLevel.None
        };
    }

    /// <summary>
    /// Checks if a hearing is required
    /// </summary>
    public bool RequiresHearing(DisciplinaryLevel level)
    {
        return level == DisciplinaryLevel.DismissalHearing;
    }

    /// <summary>
    /// Gets description for the disciplinary action
    /// </summary>
    public string GetDescription(DisciplinaryLevel level, int offenceCount)
    {
        return level switch
        {
            DisciplinaryLevel.Counselling => $"Counselling - {GetOrdinal(offenceCount)} offence",
            DisciplinaryLevel.VerbalWarning => $"Verbal Warning - {GetOrdinal(offenceCount)} offence",
            DisciplinaryLevel.WrittenWarning => $"Written Warning - {GetOrdinal(offenceCount)} offence",
            DisciplinaryLevel.FinalWrittenWarning => $"Final Written Warning - {GetOrdinal(offenceCount)} offence",
            DisciplinaryLevel.DismissalHearing => $"Dismissal Hearing - {GetOrdinal(offenceCount)} offence",
            DisciplinaryLevel.Dismissal => "Dismissal",
            _ => "No action required"
        };
    }

    private string GetOrdinal(int number)
    {
        if (number <= 0) return number.ToString();
        
        var lastDigit = number % 10;
        var lastTwoDigits = number % 100;
        
        if (lastDigit == 1 && lastTwoDigits != 11) return $"{number}st";
        if (lastDigit == 2 && lastTwoDigits != 12) return $"{number}nd";
        if (lastDigit == 3 && lastTwoDigits != 13) return $"{number}rd";
        return $"{number}th";
    }
}
