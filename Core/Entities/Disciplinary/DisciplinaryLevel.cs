namespace WACAMS.Core.Disciplinary.Models;

/// <summary>
/// Disciplinary action levels in progressive order
/// </summary>
public enum DisciplinaryLevel
{
    None = 0,
    Counselling = 1,
    VerbalWarning = 2,
    WrittenWarning = 3,
    FinalWrittenWarning = 4,
    DismissalHearing = 5,
    Dismissal = 6
}
