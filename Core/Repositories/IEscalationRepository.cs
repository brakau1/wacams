using WACAMS.Core.Entities;

namespace WACAMS.Core.Repositories;

/// <summary>
/// Repository interface for escalation data access
/// </summary>
public interface IEscalationRepository
{
    Task<Escalation?> GetByIdAsync(Guid id);
    Task<IEnumerable<Escalation>> GetByExceptionIdAsync(Guid exceptionId);
    Task<IEnumerable<Escalation>> GetByEmployeeIdAsync(string employeeId);
    Task<IEnumerable<Escalation>> GetPendingEscalationsAsync();
    Task<IEnumerable<Escalation>> GetDueForEscalationAsync(DateTime cutoffTime);
    Task<Escalation> CreateAsync(Escalation escalation);
    Task<Escalation> UpdateAsync(Escalation escalation);
    Task<bool> DeleteAsync(Guid id);
}
