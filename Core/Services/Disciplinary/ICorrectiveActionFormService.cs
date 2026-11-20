using WACAMS.Core.Disciplinary.Models;

namespace WACAMS.Core.Disciplinary.Services;

/// <summary>
/// Interface for generating corrective action form PDFs
/// </summary>
public interface ICorrectiveActionFormService
{
    /// <summary>
    /// Generates a corrective action form PDF and uploads it to storage
    /// </summary>
    /// <param name="action">The corrective action to generate form for</param>
    /// <param name="employeeName">Employee's display name</param>
    /// <param name="department">Employee's department</param>
    /// <param name="position">Employee's position</param>
    /// <param name="issuedByManager">Manager who issued the action</param>
    /// <param name="managerPosition">Manager's position</param>
    /// <param name="followUpDate">Follow-up review date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>URL of the uploaded PDF file</returns>
    Task<string> GenerateAndUploadFormAsync(
        CorrectiveAction action,
        string employeeName,
        string department,
        string position,
        string issuedByManager,
        string managerPosition,
        DateTime followUpDate,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates a corrective action form PDF and returns it as bytes
    /// </summary>
    /// <param name="action">The corrective action to generate form for</param>
    /// <param name="employeeName">Employee's display name</param>
    /// <param name="department">Employee's department</param>
    /// <param name="position">Employee's position</param>
    /// <param name="issuedByManager">Manager who issued the action</param>
    /// <param name="managerPosition">Manager's position</param>
    /// <param name="followUpDate">Follow-up review date</param>
    /// <returns>PDF file as byte array</returns>
    Task<byte[]> GenerateFormAsync(
        CorrectiveAction action,
        string employeeName,
        string department,
        string position,
        string issuedByManager,
        string managerPosition,
        DateTime followUpDate);
}
