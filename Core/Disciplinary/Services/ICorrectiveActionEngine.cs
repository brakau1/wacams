using WACAMS.Core.Disciplinary.Models;

namespace WACAMS.Core.Disciplinary.Services;

/// <summary>
/// Interface for corrective action engine
/// </summary>
public interface ICorrectiveActionEngine
{
    /// <summary>
    /// Determines and creates corrective action for an offence
    /// </summary>
    Task<DisciplinaryActionResult> DetermineActionAsync(
        string employeeId,
        Guid exceptionId,
        OffenceType offenceType,
        string issuedBy,
        string reason);

    /// <summary>
    /// Gets the current offence count for an employee
    /// </summary>
    Task<int> GetOffenceCountAsync(string employeeId, OffenceType offenceType);

    /// <summary>
    /// Acknowledges a corrective action
    /// </summary>
    Task<bool> AcknowledgeActionAsync(Guid actionId, string acknowledgedBy);

    /// <summary>
    /// Disputes a corrective action
    /// </summary>
    Task<bool> DisputeActionAsync(Guid actionId, string disputedBy, string reason);

    /// <summary>
    /// Schedules a dismissal hearing
    /// </summary>
    Task<bool> ScheduleHearingAsync(Guid actionId, DateTime hearingDate, string scheduledBy);

    /// <summary>
    /// Records hearing outcome
    /// </summary>
    Task<bool> RecordHearingOutcomeAsync(Guid actionId, string outcome, string recordedBy);
}
