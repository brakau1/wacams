using Microsoft.EntityFrameworkCore;
using WACAMS.Core.Enums;
using WACAMS.Core.Entities;
using WACAMS.Infrastructure.Data;

namespace WACAMS.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for attendance exceptions
/// </summary>
public class AttendanceExceptionRepository : IAttendanceExceptionRepository
{
    private readonly WacamContext _context;

    public AttendanceExceptionRepository(WacamContext context)
    {
        _context = context;
    }

    public async Task<AttendanceException?> GetByIdAsync(Guid id)
    {
        return await _context.Set<AttendanceException>()
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<AttendanceException>> GetByDateAsync(DateTime date, string? departmentId = null)
    {
        var query = _context.Set<AttendanceException>()
            .Where(e => e.ExceptionDate.Date == date.Date);

        if (!string.IsNullOrEmpty(departmentId) && Guid.TryParse(departmentId, out var deptGuid))
        {
            // Join with employees to filter by department
            query = query.Join(
                _context.Employees,
                e => e.EmployeeId,
                emp => emp.EmployeeId,
                (e, emp) => new { Exception = e, Employee = emp }
            )
            .Where(x => x.Employee.DepartmentId == deptGuid)
            .Select(x => x.Exception);
        }

        return await query.ToListAsync();
    }

    public async Task<List<AttendanceException>> GetByEmployeeAsync(
        string employeeId, 
        DateTime? fromDate = null, 
        DateTime? toDate = null)
    {
        var query = _context.Set<AttendanceException>()
            .Where(e => e.EmployeeId == employeeId);

        if (fromDate.HasValue)
        {
            query = query.Where(e => e.ExceptionDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(e => e.ExceptionDate <= toDate.Value);
        }

        return await query.OrderByDescending(e => e.ExceptionDate).ToListAsync();
    }

    public async Task<AttendanceException> CreateAsync(AttendanceException exception)
    {
        _context.Set<AttendanceException>().Add(exception);
        await _context.SaveChangesAsync();
        return exception;
    }

    public async Task<AttendanceException> UpdateAsync(AttendanceException exception)
    {
        _context.Set<AttendanceException>().Update(exception);
        await _context.SaveChangesAsync();
        return exception;
    }

    public async Task<int> GetPriorOccurrencesAsync(string employeeId, string exceptionType)
    {
        return await _context.Set<AttendanceException>()
            .Where(e => e.EmployeeId == employeeId 
                     && e.ExceptionType.ToString() == exceptionType 
                     && e.Status == InvestigationStatus.Investigated)
            .CountAsync();
    }
}
