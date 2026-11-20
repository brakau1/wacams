using WACAMS.Core.Disciplinary.Models;

namespace WACAMS.Core.Repositories.Disciplinary;

/// <summary>
/// Repository interface for validity periods
/// </summary>
public interface IValidityPeriodRepository
{
    Task<ValidityPeriod?> GetByLevelAsync(DisciplinaryLevel level);
    Task<List<ValidityPeriod>> GetAllAsync();
    Task<ValidityPeriod> CreateAsync(ValidityPeriod period);
    Task<ValidityPeriod> UpdateAsync(ValidityPeriod period);
}
