namespace WACAMS.Core.Enums;

/// <summary>
/// Represents the progressive disciplinary warning type issued to an employee.
/// </summary>
public enum WarningType
{
    /// <summary>
    /// Informal counselling session — first step in corrective action process.
    /// </summary>
    Counselling = 1,

    /// <summary>
    /// Formal verbal warning recorded on employee file.
    /// </summary>
    VerbalWarning = 2,

    /// <summary>
    /// Formal written warning with documented reasons and expectations.
    /// </summary>
    WrittenWarning = 3,

    /// <summary>
    /// Final written warning before potential termination or further escalation.
    /// </summary>
    FinalWrittenWarning = 4
}
