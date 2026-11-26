using Microsoft.EntityFrameworkCore;
using WACAMS.Api.Data;
using WACAMS.Core.Entities;
using WACAMS.Core.Enums;

namespace WACAMS.Api.Services;

/// <summary>
/// Service for enforcing department-based access control.
/// Determines which departments a user can access based on their role.
/// </summary>
public class DepartmentAuthorizationService
{
    private readonly WacamDbContext _context;
    private readonly ILogger<DepartmentAuthorizationService> _logger;

    public DepartmentAuthorizationService(
        WacamDbContext context,
        ILogger<DepartmentAuthorizationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets the department IDs that a user is authorized to access.
    /// </summary>
    /// <param name="employeeId">The employee ID of the user</param>
    /// <returns>List of department IDs the user can access, or null for unrestricted access</returns>
    public async Task<List<Guid>?> GetAuthorizedDepartmentIdsAsync(string employeeId)
    {
        var user = await _context.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.EmployeeId == employeeId);

        if (user == null)
        {
            _logger.LogWarning("User not found for employee ID: {EmployeeId}", employeeId);
            return new List<Guid>(); // No access
        }

        var role = user.Role;

        // SuperAdmin, Admin, HR have unrestricted access
        if (role == Enums.Role.SuperAdmin || role == Enums.Role.Admin || role == Enums.Role.HR)
        {
            return null; // null means unrestricted
        }

        // Operator can only access their own records (no department scope)
        if (role == Role.Operator)
        {
            return new List<Guid>(); // Empty list means no department access
        }

        // Supervisor and Manager can access their assigned department
        if (user.Employee != null && user.Employee.DepartmentId!= Guid.Empty)
        {
            return new List<Guid> { user.Employee.DepartmentId };
        }

        _logger.LogWarning("User {EmployeeId} with role {Role} has no department assignment", employeeId, role);
        return new List<Guid>(); // No department assigned, no access
    }

    /// <summary>
    /// Checks if a user can access a specific department.
    /// </summary>
    /// <param name="employeeId">The employee ID of the user</param>
    /// <param name="departmentId">The department ID to check</param>
    /// <returns>True if access is allowed, false otherwise</returns>
    public async Task<bool> CanAccessDepartmentAsync(string employeeId, Guid departmentId)
    {
        var authorizedDepartments = await GetAuthorizedDepartmentIdsAsync(employeeId);

        // null means unrestricted access
        if (authorizedDepartments == null)
        {
            return true;
        }

        // Empty list means no department access
        if (authorizedDepartments.Count == 0)
        {
            return false;
        }

        return authorizedDepartments.Contains(departmentId);
    }

    /// <summary>
    /// Checks if a user has unrestricted department access.
    /// </summary>
    /// <param name="employeeId">The employee ID of the user</param>
    /// <returns>True if user has unrestricted access, false otherwise</returns>
    public async Task<bool> HasUnrestrictedAccessAsync(string employeeId)
    {
        var authorizedDepartments = await GetAuthorizedDepartmentIdsAsync(employeeId);
        return authorizedDepartments == null;
    }

    /// <summary>
    /// Filters a query to only include employees from departments the user can access.
    /// </summary>
    /// <typeparam name="T">The entity type that has an Employee relationship</typeparam>
    /// <param name="query">The IQueryable to filter</param>
    /// <param name="employeeId">The employee ID of the user</param>
    /// <param name="employeeSelector">Function to select the Employee from the entity</param>
    /// <returns>Filtered query</returns>
    public async Task<IQueryable<T>> FilterByDepartmentAccessAsync<T>(
        IQueryable<T> query,
        string employeeId,
        Func<T, Employee?> employeeSelector)
    {
        var authorizedDepartments = await GetAuthorizedDepartmentIdsAsync(employeeId);

        // null means unrestricted access - return query as-is
        if (authorizedDepartments == null)
        {
            return query;
        }

        // Empty list means no department access - return empty query
        if (authorizedDepartments.Count == 0)
        {
            return query.Where(e => false);
        }

        // Filter by authorized departments
        return query.Where(e =>
        {
            var employee = employeeSelector(e);
            return employee != null &&
                   employee.DepartmentId!= Guid.Empty &&
                   authorizedDepartments.Contains(employee.DepartmentId);
        });
    }

    /// <summary>
    /// Validates that a requested departmentId is within the user's authorized scope.
    /// Throws an exception if access is denied.
    /// </summary>
    /// <param name="employeeId">The employee ID of the user</param>
    /// <param name="requestedDepartmentId">The department ID being requested</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when access is denied</exception>
    public async Task ValidateDepartmentAccessAsync(string employeeId, Guid? requestedDepartmentId)
    {
        if (!requestedDepartmentId!= Guid.Empty)
        {
            // No specific department requested - check if user has any department access
            var authorizedDepartments = await GetAuthorizedDepartmentIdsAsync(employeeId);
            if (authorizedDepartments != null && authorizedDepartments.Count == 0)
            {
                throw new UnauthorizedAccessException("User has no department access");
            }
            return;
        }

        var canAccess = await CanAccessDepartmentAsync(employeeId, requestedDepartmentId);
        if (!canAccess)
        {
            _logger.LogWarning(
                "User {EmployeeId} attempted to access unauthorized department {DepartmentId}",
                employeeId,
                requestedDepartmentId);
            throw new UnauthorizedAccessException(
                $"Access denied to department {requestedDepartmentId}");
        }
    }
}
