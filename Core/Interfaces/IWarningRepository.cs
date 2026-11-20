using WACAMS.Core.Entities;

namespace WACAMS.Core.Interfaces;

/// <summary>
/// Specialized repository for WarningLetter and Escalation operations.
/// Provides methods for retrieving warnings by employee, finding overdue reviews,
/// and managing escalation chains.
/// </summary>
public interface IWarningRepository : IRepository<WarningLetter>
{
    /// <summary>
    /// Retrieves all warning letters issued to a specific employee.
    /// Ordered by date issued descending (most recent first).
    /// Includes related escalations in the query.
    /// </summary>
    /// <param name="employeeId">The employee's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>All warning letters for the employee.</returns>
    Task<IEnumerable<WarningLetter>> GetByEmployeeAsync(
        string employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all warning letters where the NextReviewDate has passed and no follow-up action has been taken.
    /// Used for proactive review scheduling and supervisor alerts.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>All warning letters with overdue next review dates.</returns>
    Task<IEnumerable<WarningLetter>> GetOverdueReviewsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all escalations triggered by a specific warning letter.
    /// Ordered by escalation level ascending (level 1 first).
    /// </summary>
    /// <param name="warningLetterId">The GUID of the warning letter.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>All escalations for the specified warning letter.</returns>
    Task<IEnumerable<Escalation>> GetEscalationsAsync(
        Guid warningLetterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts how many active (non-closed) warnings an employee currently has at each warning type level.
    /// Used as input for progressive discipline threshold checks.
    /// </summary>
    /// <param name="employeeId">The employee's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A dictionary mapping WarningType to its count of active warnings.</returns>
    Task<Dictionary<Enums.WarningType, int>> CountActiveWarningsAsync(
        string employeeId, CancellationToken cancellationToken = default);
}
