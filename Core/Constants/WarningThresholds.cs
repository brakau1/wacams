namespace WACAMS.Core.Constants;

/// <summary>
/// Defines the progressive disciplinary warning thresholds based on exception counts per calendar month.
/// These thresholds drive the automatic warning generation logic in IWarningGenerationService.
/// Each level requires BOTH a minimum monthly exception count AND (where specified) an existing active warning at the prior level.
/// </summary>
public static class WarningThresholds
{
    /// <summary>
    /// Minimum number of attendance exceptions in a calendar month to trigger a Counselling warning.
    /// Also triggered unconditionally for employees with zero prior warnings (first-time offenders).
    /// Counselling is informal — no formal file notation unless repeated.
    /// </summary>
    public const int CounsellingExceptionCount = 3;

    /// <summary>
    /// Minimum number of attendance exceptions in a calendar month to trigger a Verbal Warning.
    /// Requires: Either 5+ exceptions this month OR an existing active Counselling warning.
    /// Verbal Warning is formally recorded on the employee's file and remains active for 30 days.
    /// NextReviewDate after issuance: 30 days from DateIssued.
    /// </summary>
    public const int VerbalWarningExceptionCount = 5;

    /// <summary>
    /// Minimum number of attendance exceptions in a calendar month to trigger a Written Warning.
    /// Requires: Either 7+ exceptions this month OR an existing active Verbal Warning.
    /// Written Warning is a formal document requiring employee signature. Remains active for 60 days.
    /// NextReviewDate after issuance: 60 days from DateIssued.
    /// </summary>
    public const int WrittenWarningExceptionCount = 7;

    /// <summary>
    /// Minimum number of attendance exceptions in a calendar month to trigger a Final Written Warning.
    /// Triggered by EITHER: 9+ exceptions this month, OR an existing active Written Warning.
    /// Final Written Warning is the last step before potential termination consideration.
    /// Automatically triggers escalation to the next manager level if no action taken within 24 hours.
    /// NextReviewDate after issuance: 90 days from DateIssued.
    /// </summary>
    public const int FinalWrittenWarningExceptionCount = 9;

    /// <summary>
    /// Time window in hours that a warning remains "active" for threshold accumulation purposes.
    /// A warning becomes inactive (expired) after this many days from its DateIssued.
    /// Used when evaluating whether an employee already has an active warning at a given level.
    /// </summary>
    public const int ActiveWarningValidityDays = 90;

    /// <summary>
    /// Time window in hours that a manager has to act on an escalation before it automatically advances.
    /// If the escalated manager does not mark the action as completed within this window,
    /// the escalation moves to the next level up the hierarchy.
    /// Set to 24 hours per organizational policy.
    /// </summary>
    public const int EscalationActionWindowHours = 24;

    /// <summary>
    /// Maximum escalation level allowed. Prevents infinite escalation chains.
    /// Level 1 = Team Leader → Supervisor
    /// Level 2 = Supervisor → Production Manager
    /// Level 3 = Production Manager → Director/Plant Head
    /// At max level, the case is automatically closed with a system notification to HR.
    /// </summary>
    public const int MaxEscalationLevel = 3;

    /// <summary>
    /// Returns the appropriate warning type for a given monthly exception count.
    /// This helper method centralizes threshold evaluation logic.
    /// Returns null if the count does not meet any threshold.
    /// </summary>
    /// <param name="exceptionCount">Total attendance exceptions in the current calendar month.</param>
    /// <returns>The WarningType to escalate to, or null if no new warning is warranted.</returns>
    public static Enums.WarningType? GetWarningTypeForExceptionCount(int exceptionCount)
    {
        if (exceptionCount >= FinalWrittenWarningExceptionCount)
            return Enums.WarningType.FinalWrittenWarning;

        if (exceptionCount >= WrittenWarningExceptionCount)
            return Enums.WarningType.WrittenWarning;

        if (exceptionCount >= VerbalWarningExceptionCount)
            return Enums.WarningType.VerbalWarning;

        if (exceptionCount >= CounsellingExceptionCount)
            return Enums.WarningType.Counselling;

        return null;
    }
}
