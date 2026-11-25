using Microsoft.EntityFrameworkCore;
using WACAMS.Api.Data;
using WACAMS.Core.Entities;

namespace WACAMS.Api.Services;

/// <summary>
/// Service to retrieve user permissions from the database.
/// </summary>
public class PermissionService
{
    private readonly WacamDbContext _context;

    public PermissionService(WacamDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all permissions for a user based on their assigned roles.
    /// </summary>
    public async Task<IEnumerable<string>> GetUserPermissionsAsync(int userId)
    {
        var permissions = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role)
            .SelectMany(r => r.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync();

        return permissions;
    }

    /// <summary>
    /// Get all permissions for a user by employee ID.
    /// </summary>
    public async Task<IEnumerable<string>> GetUserPermissionsByEmployeeIdAsync(string employeeId)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

        if (employee == null)
            return Enumerable.Empty<string>();

        return await GetUserPermissionsAsync(employee.Id);
    }

    /// <summary>
    /// Check if a user has a specific permission.
    /// </summary>
    public async Task<bool> HasPermissionAsync(int userId, string permission)
    {
        var permissions = await GetUserPermissionsAsync(userId);
        return permissions.Contains(permission);
    }

    /// <summary>
    /// Check if a user has any of the specified permissions.
    /// </summary>
    public async Task<bool> HasAnyPermissionAsync(int userId, IEnumerable<string> permissions)
    {
        var userPermissions = await GetUserPermissionsAsync(userId);
        return userPermissions.Intersect(permissions).Any();
    }
}
