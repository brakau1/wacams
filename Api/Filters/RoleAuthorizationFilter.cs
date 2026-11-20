using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Wacam.Api.Middleware;

namespace Wacam.Api.Filters;

/// <summary>
/// Attribute to enforce role requirement on endpoints beyond standard [Authorize].
/// Usage: [RoleRequirement("SuperAdmin")] on controller or action.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RoleRequirementAttribute : Attribute, IAuthorizationFilter
{
    public string RequiredRole { get; }

    public RoleRequirementAttribute(string requiredRole)
    {
        RequiredRole = requiredRole;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Skip if already handled by ASP.NET Core authorization pipeline
        if (context.Result is not null)
            return;

        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated == true)
            return;

        var userRole = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(userRole))
            return;

        var roles = new Dictionary<string, int>
        {
            { "SuperAdmin", 7 },
            { "Admin", 6 },
            { "HR", 5 },
            { "Manager", 4 },
            { "Supervisor", 3 },
            { "Operator", 2 },
            { "User", 1 }
        };

        var userLevel = roles.GetValueOrDefault(userRole, 0);
        var requiredLevel = roles.GetValueOrDefault(RequiredRole, 0);

        if (userLevel < requiredLevel)
        {
            context.Result = new ForbidResult();
        }
    }
}
