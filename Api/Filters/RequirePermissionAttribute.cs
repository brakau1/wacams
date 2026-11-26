using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Wacam.Api.Filters;

/// <summary>
/// Attribute to enforce permission requirement on endpoints.
/// Usage: [RequirePermission("attendance.view.team")] on controller or action.
/// Supports multiple permissions (any match grants access).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
{
    public string[] RequiredPermissions { get; }

    public RequirePermissionAttribute(params string[] permissions)
    {
        RequiredPermissions = permissions;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (!(user.Identity?.IsAuthenticated ?? false))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Admin role bypasses all permission checks
        var role = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (role == "Admin" || role == "SuperAdmin")
            return;

        // Check if user has any of the required permissions
        var userPermissions = user.FindAll("permission").Select(c => c.Value).ToHashSet();

        var hasPermission = RequiredPermissions.Any(p => userPermissions.Contains(p));

        if (!hasPermission)
        {
            context.Result = new ForbidResult();
        }
    }
}
