namespace WACAMS.Core.Enums;

/// <summary>
/// Represents the employment status of an employee.
/// </summary>
public enum EmployeeStatus
{
    /// <summary>
    /// Active employee currently employed and eligible for scheduling.
    /// </summary>
    Active = 1,

    /// <summary>
    /// On approved leave (sick, annual, unpaid) — still employed but temporarily inactive.
    /// </summary>
    OnLeave = 2,

    /// <summary>
    /// Terminated or resigned — no longer part of the active workforce.
    /// </summary>
    Terminated = 3,

    /// <summary>
    /// Suspended pending investigation — access restricted but not yet terminated.
    /// </summary>
    Suspended = 4
}
