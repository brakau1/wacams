using Microsoft.EntityFrameworkCore;
using WACAMS.Core.Disciplinary.Models;
using WACAMS.Core.Disciplinary.Repositories;
using WACAMS.Infrastructure.Data;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for offence counters
/// </summary>
public class OffenceCounterRepository : IOffenceCounterRepository
{
    private readonly WacamDbContext _context;

    public OffenceCounterRepository(WacamDbContext context)
    {
        _context = context;
    }

    public async Task<OffenceCounter?> GetByEmployeeAndTypeAsync(string employeeId, OffenceType offenceType)
    {
        return await _context.Set<OffenceCounter>()
            .FirstOrDefaultAsync(c => c.EmployeeId == employeeId && c.OffenceType == offenceType);
    }

    public async Task<List<OffenceCounter>> GetByEmployeeAsync(string employeeId)
    {
        return await _context.Set<OffenceCounter>()
            .Where(c => c.EmployeeId == employeeId)
            .ToListAsync();
    }

    public async Task<OffenceCounter> CreateAsync(OffenceCounter counter)
    {
        _context.Set<OffenceCounter>().Add(counter);
        await _context.SaveChangesAsync();
        return counter;
    }

    public async Task<OffenceCounter> UpdateAsync(OffenceCounter counter)
    {
        _context.Set<OffenceCounter>().Update(counter);
        await _context.SaveChangesAsync();
        return counter;
    }
}
