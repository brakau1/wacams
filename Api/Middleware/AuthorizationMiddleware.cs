using Wacam.Api.Filters;
using Wacam.Api.Utils;

namespace Wacam.Api.Middleware;

/// <summary>
/// Post-authz check for RBAC enforcement per endpoint.
/// Provides additional role-based and permission-based checks beyond [Authorize] attribute.
/// </summary>
public class AuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthorizationMiddleware> _logger;

    public AuthorizationMiddleware(RequestDelegate next, ILogger<AuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only enforce if user is authenticated
        if (!context.User.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        var endpoint = context.GetEndpoint();
        var employeeId = TokenGenerator.GetEmployeeIdFromToken(context.User);
        var userRole = TokenGenerator.GetRoleFromToken(context.User);

        // Check role-based authorization (legacy hierarchical)
        var requiredRole = endpoint?.Metadata
            .GetMetadata<RoleRequirementMetadata>()?.RequiredRole;

        if (requiredRole != null)
        {
            if (!IsAuthorized(userRole, requiredRole))
            {
                _logger.LogWarning("User {EmployeeId} with role {UserRole} denied access to {Path}. Required role: {RequiredRole}",
                    employeeId, userRole, context.Request.Path, requiredRole);
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { error = "Forbidden", message = $"Role '{userRole}' does not have access to this resource." });
                return;
            }
        }

        // Check permission-based authorization
        var requiredPermissions = endpoint?.Metadata
            .GetMetadata<RequirePermissionAttribute>()?.RequiredPermissions;

        if (requiredPermissions != null && requiredPermissions.Length > 0)
        {
            // Admin role bypasses all permission checks
            if (userRole != "Admin" && userRole != "SuperAdmin")
            {
                var userPermissions = TokenGenerator.GetPermissionsFromToken(context.User).ToHashSet();
                var hasPermission = requiredPermissions.Any(p => userPermissions.Contains(p));

                if (!hasPermission)
                {
                    _logger.LogWarning("User {EmployeeId} with role {UserRole} denied access to {Path}. Required permissions: {RequiredPermissions}",
                        employeeId, userRole, context.Request.Path, string.Join(", ", requiredPermissions));
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new { error = "Forbidden", message = "You do not have the required permissions to access this resource." });
                    return;
                }
            }
        }

        await _next(context);
    }

    /// <summary>
    /// Check if a user role has permission based on hierarchy.
    /// SuperAdmin > Admin > HR > Manager > Supervisor > Operator
    /// </summary>
    private static bool IsAuthorized(string userRole, string requiredRole)
    {
        var roles = new Dictionary<string, int>
        {
            { "SuperAdmin", 6 },
            { "Admin", 5 },
            { "HR", 4 },
            { "Manager", 3 },
            { "Supervisor", 2 },
            { "Operator", 1 }
        };

        var userLevel = roles.GetValueOrDefault(userRole, 0);
        var requiredLevel = roles.GetValueOrDefault(requiredRole, 0);

        return userLevel >= requiredLevel;
    }
}

public class RoleRequirementMetadata
{
    public string RequiredRole { get; set; } = string.Empty;

    public RoleRequirementMetadata(string role)
    {
        RequiredRole = role;
    }
}
