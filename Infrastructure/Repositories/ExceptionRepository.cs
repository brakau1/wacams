using Microsoft.EntityFrameworkCore;
using WACAMS.Core.Enums;
using WACAMS.Core.Entities;
using WACAMS.Core.Exceptions.Interfaces;
using WACAMS.Infrastructure.Data;

namespace WACAMS.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for attendance exceptions
/// </summary>
public class ExceptionRepository : IExceptionRepository
{
    private readonly WacamContext _context;

    public ExceptionRepository(WacamContext context)
    {
        _context = context;
    }

    public async Task<AttendanceException?> GetByIdAsync(Guid id)
    {
        return await _context.AttendanceExceptions
            .Include(e => e.Employee)
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
            query = query.Where(e => e.Date >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(e => e.Date <= endDate.Value);
        }

        return await query.OrderByDescending(e => e.Date).ToListAsync();
    }

    public async Task<IEnumerable<AttendanceException>> GetPendingExceptionsAsync()
    {
        return await _context.AttendanceExceptions
            .Include(e => e.Employee)
            .Where(e => e.InvestigationStatus == InvestigationStatus.Open ||
                        e.InvestigationStatus == InvestigationStatus.UnderReview)
            .OrderBy(e => e.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<AttendanceException>> GetByDateRangeAsync(
        DateTime startDate, 
        DateTime endDate)
    {
        return await _context.AttendanceExceptions
            .Include(e => e.Employee)
            .Where(e => e.Date >= startDate && e.Date <= endDate)
            .OrderBy(e => e.Date)
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
