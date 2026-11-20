using Microsoft.EntityFrameworkCore;
using WACAMS.Core.Disciplinary.Models;
using WACAMS.Core.Disciplinary.Repositories;
using WACAMS.Infrastructure.Data;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for corrective actions
/// </summary>
public class CorrectiveActionRepository : ICorrectiveActionRepository
{
    private readonly WacamContext _context;

    public CorrectiveActionRepository(WacamContext context)
    {
        _context = context;
    }

    public async Task<CorrectiveAction?> GetByIdAsync(Guid id)
    {
        return await _context.Set<CorrectiveAction>()
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<CorrectiveAction>> GetByEmployeeAsync(string employeeId, bool includeExpired = false)
    {
        var query = _context.Set<CorrectiveAction>()
            .Where(a => a.EmployeeId == employeeId);

        if (!includeExpired)
        {
            query = query.Where(a => !a.IsExpired);
        }

        return await query.OrderByDescending(a => a.IssuedAt).ToListAsync();
    }

    public async Task<List<CorrectiveAction>> GetByExceptionAsync(Guid exceptionId)
    {
        return await _context.Set<CorrectiveAction>()
            .Where(a => a.ExceptionId == exceptionId)
            .ToListAsync();
    }

    public async Task<List<CorrectiveAction>> GetExpiredActionsAsync()
    {
        return await _context.Set<CorrectiveAction>()
            .Where(a => !a.IsExpired && a.ValidUntil < DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task<CorrectiveAction> CreateAsync(CorrectiveAction action)
    {
        _context.Set<CorrectiveAction>().Add(action);
        await _context.SaveChangesAsync();
        return action;
    }

    public async Task<CorrectiveAction> UpdateAsync(CorrectiveAction action)
    {
        _context.Set<CorrectiveAction>().Update(action);
        await _context.SaveChangesAsync();
        return action;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Set<CorrectiveAction>()
            .AnyAsync(a => a.Id == id);
    }
}
