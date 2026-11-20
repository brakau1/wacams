using WACAMS.Core.Entities;

namespace WACAMS.Core.Services;

/// <summary>
/// Domain service responsible for generating progressive disciplinary warning letters.
/// Implements the escalation ladder: Counselling → Verbal Warning → Written Warning → Final Written Warning.
/// WARNING: All methods enforce the rule that a warning can ONLY be generated after an
/// AttendanceException has status "Investigated" or "Closed". If no such exception exists,
/// the method returns null and does not create any warning letter.
/// </summary>
public interface IWarningGenerationService
{
    /// <summary>
    /// Generates a Counselling warning — the first step in the corrective action process.
    /// Triggered when an employee has 3+ exceptions in a calendar month, OR when this is their first warning ever.
    /// The reason references specific attended exceptions with dates and types.
    /// </summary>
    /// <param name="employeeId">The employee receiving the counselling.</param>
    /// <param name="reason">Detailed reason referencing specific attendance exceptions.</param>
    /// <param name="issuedBy">Employee ID of the supervisor/HR issuing the counselling.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A WarningLetter entity if conditions are met; null if investigation is incomplete.</returns>
    Task<WarningLetter?> GenerateCounselling(
        string employeeId,
        string reason,
        string issuedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a Verbal Warning — second step in progressive discipline.
    /// Triggered when an employee has 5+ exceptions in a calendar month, OR already has an active Counselling.
    /// Includes reference to prior counselling and continued pattern of violations.
    /// Sets NextReviewDate to 30 days from DateIssued.
    /// </summary>
    /// <param name="employeeId">The employee receiving the verbal warning.</param>
    /// <param name="reason">Detailed reason including prior counselling reference and current exceptions.</param>
    /// <param name="priorWarnings">Collection of existing warnings against this employee for context.</param>
    /// <param name="issuedBy">Employee ID of the supervisor issuing the verbal warning.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A WarningLetter entity if conditions are met; null if investigation is incomplete.</returns>
    Task<WarningLetter?> GenerateVerbalWarning(
        string employeeId,
        string reason,
        IEnumerable<WarningLetter> priorWarnings,
        string issuedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a Written Warning — third step in progressive discipline.
    /// Triggered when an employee has 7+ exceptions in a calendar month, OR already has an active Verbal Warning.
    /// Includes formal language about potential consequences of further violations.
    /// Sets NextReviewDate to 60 days from DateIssued.
    /// </summary>
    /// <param name="employeeId">The employee receiving the written warning.</param>
    /// <param name="reason">Detailed reason citing prior warnings and continued non-compliance.</param>
    /// <param name="priorWarnings">Collection of existing warnings against this employee for context.</param>
    /// <param name="issuedBy">Employee ID of the supervisor issuing the written warning.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A WarningLetter entity if conditions are met; null if investigation is incomplete.</returns>
    Task<WarningLetter?> GenerateWrittenWarning(
        string employeeId,
        string reason,
        IEnumerable<WarningLetter> priorWarnings,
        string issuedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a Final Written Warning — last step before termination consideration.
    /// Triggered when an employee has 9+ exceptions in a calendar month, OR already has an active Written Warning.
    /// Includes explicit statement that further violations may result in employment termination.
    /// Sets NextReviewDate to 90 days from DateIssued.
    /// Triggers automatic escalation to next manager level via IEscalationService.
    /// </summary>
    /// <param name="employeeId">The employee receiving the final written warning.</param>
    /// <param name="reason">Detailed reason citing all prior warnings and critical threshold breach.</param>
    /// <param name="priorWarnings">Collection of existing warnings against this employee for context.</param>
    /// <param name="issuedBy">Employee ID of the supervisor issuing the final written warning.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A WarningLetter entity if conditions are met; null if investigation is incomplete.</returns>
    Task<WarningLetter?> GenerateFinalWrittenWarning(
        string employeeId,
        string reason,
        IEnumerable<WarningLetter> priorWarnings,
        string issuedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a given warning letter should trigger an automatic escalation.
    /// Evaluates based on: (a) type is Final Written Warning, (b) no action taken within 24 hours,
    /// (c) manager has a valid ManagerId in the employee hierarchy.
    /// Returns true if escalation conditions are met, false otherwise.
    /// Used by the domain service layer before calling IEscalationService.CreateEscalation.
    /// </summary>
    /// <param name="warningLetterId">The GUID of the warning letter to evaluate.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if escalation should be triggered; false otherwise.</returns>
    Task<bool> CheckEscalationTrigger(
        Guid warningLetterId,
        CancellationToken cancellationToken = default);
}
