using Microsoft.EntityFrameworkCore;
using WACAMS.Core.Exceptions.Interfaces;
using WACAMS.Core.Exceptions.Models;
using WACAMS.Infrastructure.Data;

namespace WACAMS.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for attendance exceptions
/// </summary>
public class ExceptionRepository : IExceptionRepository
{
    private readonly ApplicationDbContext _context;

    public ExceptionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AttendanceException?> GetByIdAsync(Guid id)
    {
        return await _context.AttendanceExceptions
            .Include(e => e.Employee)
            .Include(e => e.CorrectiveAction)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<AttendanceException>> GetByEmployeeIdAsync(
        string employeeId, 
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        var query = _context.AttendanceExceptions
            .Include(e => e.Employee)
            .Where(e => e.EmployeeId == employeeId);

        if (startDate.HasValue)
        {
            query = query.Where(e => e.ExceptionDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.ExceptionDate <= endDate.Value);
        }

        return await query.OrderByDescending(e => e.ExceptionDate).ToListAsync();
    }

    public async Task<IEnumerable<AttendanceException>> GetPendingExceptionsAsync()
    {
        return await _context.AttendanceExceptions
            .Include(e => e.Employee)
            .Where(e => e.Status == ApprovalStatus.Pending)
            .OrderBy(e => e.ExceptionDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<AttendanceException>> GetByDateRangeAsync(
        DateTime startDate, 
        DateTime endDate)
    {
        return await _context.AttendanceExceptions
            .Include(e => e.Employee)
            .Where(e => e.ExceptionDate >= startDate && e.ExceptionDate <= endDate)
            .OrderBy(e => e.ExceptionDate)
            .ToListAsync();
    }

    public async Task<AttendanceException> CreateAsync(AttendanceException exception)
    {
        _context.AttendanceExceptions.Add(exception);
        await _context.SaveChangesAsync();
        return exception;
    }

    public async Task<AttendanceException> UpdateAsync(AttendanceException exception)
    {
        _context.AttendanceExceptions.Update(exception);
        await _context.SaveChangesAsync();
        return exception;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var exception = await _context.AttendanceExceptions.FindAsync(id);
        if (exception == null)
        {
            return false;
        }

        _context.AttendanceExceptions.Remove(exception);
        await _context.SaveChangesAsync();
        return true;
    }
}
