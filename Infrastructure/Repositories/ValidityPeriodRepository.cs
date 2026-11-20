using Microsoft.EntityFrameworkCore;
using WACAMS.Core.Disciplinary.Models;
using WACAMS.Core.Disciplinary.Repositories;
using WACAMS.Infrastructure.Data;

namespace WACAMS.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for validity periods
/// </summary>
public class ValidityPeriodRepository : IValidityPeriodRepository
{
    private readonly WacamContext _context;

    public ValidityPeriodRepository(WacamContext context)
    {
        _context = context;
    }

    public async Task<ValidityPeriod?> GetByLevelAsync(DisciplinaryLevel level)
    {
        return await _context.Set<ValidityPeriod>()
            .FirstOrDefaultAsync(v => v.Level == level && v.IsActive);
    }

    public async Task<List<ValidityPeriod>> GetAllAsync()
    {
        return await _context.Set<ValidityPeriod>()
            .Where(v => v.IsActive)
            .OrderBy(v => v.Level)
            .ToListAsync();
    }

    public async Task<ValidityPeriod> CreateAsync(ValidityPeriod period)
    {
        _context.Set<ValidityPeriod>().Add(period);
        await _context.SaveChangesAsync();
        return period;
    }

    public async Task<ValidityPeriod> UpdateAsync(ValidityPeriod period)
    {
        _context.Set<ValidityPeriod>().Update(period);
        await _context.SaveChangesAsync();
        return period;
    }
}
