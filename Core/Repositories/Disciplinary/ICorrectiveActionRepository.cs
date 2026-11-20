using WACAMS.Core.Disciplinary.Models;

namespace WACAMS.Core.Repositories.Disciplinary;

/// <summary>
/// Repository interface for corrective actions
/// </summary>
public interface ICorrectiveActionRepository
{
    Task<CorrectiveAction?> GetByIdAsync(Guid id);
    Task<List<CorrectiveAction>> GetByEmployeeAsync(string employeeId, bool includeExpired = false);
    Task<List<CorrectiveAction>> GetByExceptionAsync(Guid exceptionId);
    Task<List<CorrectiveAction>> GetExpiredActionsAsync();
    Task<CorrectiveAction> CreateAsync(CorrectiveAction action);
    Task<CorrectiveAction> UpdateAsync(CorrectiveAction action);
    Task<bool> ExistsAsync(Guid id);
}
