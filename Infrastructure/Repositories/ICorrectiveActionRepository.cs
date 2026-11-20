using WACAMS.Core.RulesEngine.Models;

namespace Infrastructure.Repositories;

/// <summary>
/// Interface for corrective action repository
/// </summary>
public interface ICorrectiveActionRepository
{
    Task<CorrectiveAction?> GetByIdAsync(Guid id);
    Task<List<CorrectiveAction>> GetByEmployeeAsync(string employeeId, bool? isActive = null);
    Task<List<CorrectiveAction>> GetByExceptionAsync(Guid exceptionId);
    Task<CorrectiveAction> CreateAsync(CorrectiveAction action);
    Task<CorrectiveAction> UpdateAsync(CorrectiveAction action);
    Task<int> GetPriorActionsCountAsync(string employeeId, string exceptionType);
}
