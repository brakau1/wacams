using WACAMS.Core.Disciplinary.Models;

namespace WACAMS.Core.Disciplinary.Repositories;

/// <summary>
/// Repository interface for offence counters
/// </summary>
public interface IOffenceCounterRepository
{
    Task<OffenceCounter?> GetByEmployeeAndTypeAsync(string employeeId, OffenceType offenceType);
    Task<List<OffenceCounter>> GetByEmployeeAsync(string employeeId);
    Task<OffenceCounter> CreateAsync(OffenceCounter counter);
    Task<OffenceCounter> UpdateAsync(OffenceCounter counter);
}
