using WACAMS.Core.Entities;

namespace WACAMS.Core.Interfaces;

/// <summary>
/// Specialized repository for Employee entity operations.
/// Provides methods optimized for workforce management queries including hierarchy traversal.
/// </summary>
public interface IEmployeeRepository : IRepository<Employee>
{
    /// <summary>
    /// Retrieves an employee by their master business key (EmployeeId).
    /// This is the most common lookup — used across all attendance, warnings, and user lookups.
    /// </summary>
    /// <param name="employeeId">The employee's unique identifier (e.g., "708513").</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The employee if found; null otherwise.</returns>
    Task<Employee?> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all employees assigned to a specific department.
    /// </summary>
    /// <param name="departmentId">The GUID of the department.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>All employees in the specified department.</returns>
    Task<IEnumerable<Employee>> GetByDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all active employees (excluding Terminated and Suspended statuses).
    /// Used for dropdowns, roster displays, and attendance monitoring.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>All active employees.</returns>
    Task<IEnumerable<Employee>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches employees by name, employee ID, or email with fuzzy matching.
    /// Supports partial text search across DisplayName, EmployeeId, and Email fields.
    /// </summary>
    /// <param name="searchTerm">Text to search for (partial match supported).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>All employees matching the search criteria.</returns>
    Task<IEnumerable<Employee>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an employee along with their complete manager hierarchy chain.
    /// Returns the employee plus all ancestors up to the top-level manager.
    /// Used for escalation chain resolution.
    /// </summary>
    /// <param name="employeeId">The employee's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A tuple containing the employee and their full manager hierarchy list.</returns>
    Task<(Employee Employee, List<Employee> Hierarchy)> GetWithManagerHierarchyAsync(
        string employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all direct reports of a given manager (employees whose ManagerId matches).
    /// Used for supervisor dashboards showing team members.
    /// </summary>
    /// <param name="managerId">The manager's EmployeeId.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>All direct reports of the specified manager.</returns>
    Task<IEnumerable<Employee>> GetDirectReportsAsync(string managerId, CancellationToken cancellationToken = default);
}
