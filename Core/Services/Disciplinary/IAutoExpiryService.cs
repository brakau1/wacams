namespace WACAMS.Core.Disciplinary.Services;

/// <summary>
/// Interface for auto-expiry service
/// </summary>
public interface IAutoExpiryService
{
    /// <summary>
    /// Processes expired corrective actions
    /// </summary>
    Task<int> ProcessExpiredActionsAsync();

    /// <summary>
    /// Checks if a specific action has expired
    /// </summary>
    Task<bool> IsExpiredAsync(Guid actionId);

    /// <summary>
    /// Gets actions expiring within specified days
    /// </summary>
    Task<List<Models.CorrectiveAction>> GetExpiringSoonAsync(int days);
}
