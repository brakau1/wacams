using WACAMS.Core.Disciplinary.Models;

namespace WACAMS.Core.Disciplinary.Services;

/// <summary>
/// Interface for disciplinary service operations
/// </summary>
public interface IDisciplinaryService
{
    /// <summary>
    /// Generates a corrective action for an offence
    /// </summary>
    Task<CorrectiveAction> GenerateCorrectiveActionAsync(
        string employeeId,
        OffenceType offenceType,
        DateTime offenceDate,
        string issuedBy,
        string reason);

    /// <summary>
    /// Cancels a corrective action
    /// </summary>
    Task CancelCorrectiveActionAsync(
        Guid actionId,
        string cancelledBy,
        string reason);

    /// <summary>
    /// Gets the count of prior actions for an employee
    /// </summary>
    Task<int> GetPriorActionsCountAsync(string employeeId, string offenceType);
}
