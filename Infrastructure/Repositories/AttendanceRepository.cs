using Microsoft.EntityFrameworkCore;
using WACAMS.Core.Entities;
using WACAMS.Core.Interfaces;
using WACAMS.Infrastructure.Data;

namespace Infrastructure.Repositories;

/// <summary>
/// Specialized repository for AttendanceRecord operations with analytics capabilities.
/// Provides methods for date-range queries, daily statistics, and lost-time calculations.
/// </summary>
public class AttendanceRepository : Repository<AttendanceRecord>, IAttendanceRepository
{
    public AttendanceRepository(WacamContext context) : base(context) { }

    /// <inheritdoc />
    public override async Task<IEnumerable<AttendanceRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Include(ar => ar.Employee)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AttendanceRecord>> GetByDateRangeAsync(
        string employeeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Where(ar => ar.EmployeeId == employeeId && ar.Date >= startDate && ar.Date <= endDate)
            .OrderBy(ar => ar.Date)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Dictionary<WACAMS.Core.Enums.AttendanceStatus, int>> GetDailyStatsAsync(
        DateTime date, CancellationToken cancellationToken = default)
    {
        var records = await _dbSet.AsNoTracking()
            .Where(ar => ar.Date == date)
            .ToListAsync(cancellationToken);

        return records.GroupBy(ar => ar.Status)
            .ToDictionary(
                g => g.Key,
                g => g.Count());
    }

    /// <inheritdoc />
    public async Task<Dictionary<DateTime, int>> CalculateLostMinutesAsync(
        string employeeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var records = await _dbSet.AsNoTracking()
            .Where(ar => ar.EmployeeId == employeeId && ar.Date >= startDate && ar.Date <= endDate)
            .Select(ar => new { ar.Date, Lost = ar.LateMinutes + ar.EarlyMinutes })
            .ToListAsync(cancellationToken);

        return records.Where(r => r.Lost > 0)
            .ToDictionary(r => r.Date, r => r.Lost);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AttendanceException>> GetExceptionsByEmployeeAsync(
        string employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.AttendanceExceptions.AsNoTracking()
            .Where(ae => ae.EmployeeId == employeeId)
            .OrderByDescending(ae => ae.Date)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<decimal> CalculateDepartmentAttendancePercentageAsync(
        Guid departmentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        // Get all employees in the department
        var employees = await _context.Employees.AsNoTracking()
            .Where(e => e.DepartmentId == departmentId && e.Status == Core.Enums.EmployeeStatus.Active)
            .Select(e => e.EmployeeId)
            .ToListAsync(cancellationToken);

        if (!employees.Any())
            return 0m;

        // Count total present days across all employees in the range
        var totalPresentDays = await _dbSet.AsNoTracking()
            .Where(ar => employees.Contains(ar.EmployeeId) && ar.Date >= startDate && ar.Date <= endDate && ar.Status == Core.Enums.AttendanceStatus.Present)
            .CountAsync(cancellationToken);

        // Total working days = number of employees × number of unique dates in range
        var totalWorkingDays = employees.Count * (endDate - startDate).Days + 1;

        if (totalWorkingDays == 0)
            return 0m;

        return Math.Round((decimal)totalPresentDays / totalWorkingDays * 100m, 2);
    }

    /// <inheritdoc />
    public async Task<int> CountExceptionsInMonthAsync(
        string employeeId, int year, int month, CancellationToken cancellationToken = default)
    {
        var startOfMonth = new DateTime(year, month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        return await _context.AttendanceExceptions.AsNoTracking()
            .Where(ae => ae.EmployeeId == employeeId && ae.Date >= startOfMonth && ae.Date <= endOfMonth)
            .CountAsync(cancellationToken);
    }
}
