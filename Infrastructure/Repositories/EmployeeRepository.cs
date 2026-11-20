using Microsoft.EntityFrameworkCore;
using WACAMS.Core.Entities;
using WACAMS.Core.Enums;
using WACAMS.Core.Interfaces;
using WACAMS.Infrastructure.Data;

namespace Infrastructure.Repositories;

/// <summary>
/// Specialized repository for Employee entity operations.
/// Provides methods optimized for workforce management queries including hierarchy traversal.
/// </summary>
public class EmployeeRepository : Repository<Employee>, IEmployeeRepository
{
    public EmployeeRepository(WacamContext context) : base(context) { }

    /// <inheritdoc />
    public override async Task<IEnumerable<Employee>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Include(e => e.Department)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override async Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Employee?> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Employee>> GetByDepartmentAsync(
        Guid departmentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Where(e => e.DepartmentId == departmentId)
            .OrderBy(e => e.DisplayName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Employee>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Where(e => e.Status == EmployeeStatus.Active)
            .Include(e => e.Department)
            .OrderBy(e => e.DisplayName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Employee>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllActiveAsync(cancellationToken);

        var term = searchTerm.Trim().ToLowerInvariant();
        return await _dbSet.AsNoTracking()
            .Where(e =>
                e.Status == EmployeeStatus.Active &&
                (e.DisplayName.ToLower().Contains(term) ||
                 e.EmployeeId.Contains(term) ||
                 (e.Email != null && e.Email.ToLower().Contains(term))))
            .Include(e => e.Department)
            .OrderByDescending(e => e.DisplayName.Equals(searchTerm.Trim(), StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(e => e.DisplayName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(Employee Employee, List<Employee> Hierarchy)> GetWithManagerHierarchyAsync(
        string employeeId, CancellationToken cancellationToken = default)
    {
        // Fetch the employee first
        var employee = await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId, cancellationToken);

        if (employee == null)
            return (null!, []);

        // Walk up the manager chain
        var hierarchy = new List<Employee>();
        var currentManagerId = employee.ManagerId;

        while (!string.IsNullOrEmpty(currentManagerId))
        {
            var manager = await _dbSet.AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == currentManagerId, cancellationToken);

            if (manager == null)
                break;

            hierarchy.Add(manager);
            currentManagerId = manager.ManagerId;
        }

        return (employee, hierarchy);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Employee>> GetDirectReportsAsync(
        string managerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Where(e => e.ManagerId == managerId && e.Status == EmployeeStatus.Active)
            .Include(e => e.Department)
            .OrderBy(e => e.DisplayName)
            .ToListAsync(cancellationToken);
    }
}
