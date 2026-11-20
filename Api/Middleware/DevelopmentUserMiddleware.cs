using System.Security.Claims;

namespace Api.Middleware;

/// <summary>
/// Development-only middleware that injects a fake authenticated user into every request.
/// This bypasses JWT authentication for local development and UAT testing.
/// 
/// SECURITY: This middleware MUST ONLY run in Development environment.
/// It will throw an exception if used in Staging, Production, or Release builds.
/// </summary>
public class DevelopmentUserMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _environment;

    public DevelopmentUserMiddleware(RequestDelegate next, IHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // CRITICAL SECURITY CHECK: Never run outside Development
        if (!_environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "DevelopmentUserMiddleware is ONLY allowed in Development environment. " +
                "This is a security violation. Remove this middleware from Staging/Production.");
        }

        // Create development user claims
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "DEV001"),
            new Claim(ClaimTypes.Name, "Development User"),
            new Claim(ClaimTypes.Email, "developer@wacams.local"),
            new Claim(ClaimTypes.Role, "SuperAdmin"),
            new Claim("EmployeeId", "DEV001"),
            new Claim("DepartmentId", "ADMIN")
        };

        var identity = new ClaimsIdentity(claims, "DevelopmentBypass");
        context.User = new ClaimsPrincipal(identity);

        await _next(context);
    }
}
